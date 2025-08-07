using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.SystemInfo;
using PuppeteerSharp;
using System.Security.Cryptography;
using System.Text;

namespace Nastya_Archiving_project.Services.files
{
    public class FileServices : BaseServices , IFilesServices
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ISystemInfoServices _systemInfo;
        private readonly IConfiguration _configuration;
        private readonly IEncryptionServices _encryptionServices;
        public FileServices(AppDbContext context,
                            IHttpContextAccessor httpcontext,
                            ISystemInfoServices systeminfo,
                            IConfiguration configuration,
                            IEncryptionServices encryptionServices) : base(null, context)
        {
            _context = context;
            _httpContext = httpcontext;
            _systemInfo = systeminfo;
            _configuration = configuration;
            _encryptionServices = encryptionServices;
        }

        //upload single pdf file and encrypt it using AES encryption
        public async Task<(string? file, long fileSize, string? error)> upload(FileViewForm fileForm)
        {
            // Get the user's info
            var userId = (await _systemInfo.GetUserId()).Id;
            if (string.IsNullOrEmpty(userId))
                return (null, 0, "User ID is not available.");

            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if (user == null)
                return (null, 0, "User not found.");

            var group = _context.Usersgroups.FirstOrDefault(g => g.groupid == user.GroupId);
            if(group == null)
                return (null, 0, "User group not found.");
            var depr = _context.GpDepartments.FirstOrDefault(d => d.Id == user.DepariId);

            var file = fileForm.File;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Accept PDF and Word files
            var isPdf = extension == ".pdf" && file.ContentType == "application/pdf";
            var isDocx = extension == ".docx" &&
                (file.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                 file.ContentType == "application/octet-stream" ||
                 file.ContentType == "application/zip");
            var isDoc = extension == ".doc" &&
                (file.ContentType == "application/msword" ||
                 file.ContentType == "application/octet-stream");

            if (!isPdf && !isDocx && !isDoc)
                return (null, 0, $"File '{file.FileName}' is not a PDF or Word document.");

            long fileSize = file.Length;
            var storePath = await _context.PArcivingPoints.FirstOrDefaultAsync(s => s.AccountUnitId == user.AccountUnitId && s.DepartId == user.DepariId);

            string attachmentsDir = Path.Combine(
                storePath.StorePath,
                DateTime.Now.Year.ToString(),
                depr.Dscrp,
                DateTime.Now.Month.ToString(),
                _encryptionServices.DecryptString256Bit(group.Groupdscrp)
            );

            if (!Directory.Exists(attachmentsDir))
                Directory.CreateDirectory(attachmentsDir);

            var fileName = $"{await _systemInfo.GetLastRefNo()}-{Guid.NewGuid()}{extension}.gz";
            var filePath = Path.Combine(attachmentsDir, fileName);

            byte[] key = Convert.FromBase64String(_configuration["FileEncrypt:key"]);
            byte[] iv = Convert.FromBase64String(_configuration["FileEncrypt:iv"]);

            using (var inputStream = file.OpenReadStream())
            {
                using (var compressedStream = new MemoryStream())
                {
                    await CompressToGZipAsync(inputStream, compressedStream);
                    compressedStream.Position = 0;
                    await EncryptStreamToFileAsync(compressedStream, filePath, key, iv);
                }
            }

            var webPath = Path.Combine(
                storePath.StorePath.Replace(Path.DirectorySeparatorChar, '/'),
                DateTime.Now.Year.ToString(),
                depr.Dscrp,
                DateTime.Now.Month.ToString(),
                _encryptionServices.DecryptString256Bit(group.Groupdscrp),
                fileName
            ).Replace(Path.DirectorySeparatorChar, '/');

            return (webPath, fileSize, null);
        }



        public long GetFileSize(IFormFile file)
        {
            if (file == null)
                return 0;
            return file.Length;
        }

        public async Task<bool> RemoveAllTempFolderFilesAsync()
        {
            var userId = (await _systemInfo.GetUserId()).Id;
            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            var userFolder = _encryptionServices.DecryptString256Bit(user?.Realname) ?? "UnknownUser";

            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", userFolder);
            if (!Directory.Exists(tempDir))
                return true; // Nothing to delete

            try
            {
                var files = Directory.GetFiles(tempDir);
                Parallel.ForEach(files, file =>
                {
                    try { File.Delete(file); } catch { /* Ignore errors for individual files */ }
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<(string FileName, long FileSize)>> GetTempFolderFilesAsync()
        {
            var userId = (await _systemInfo.GetUserId()).Id;
            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            var userFolder = _encryptionServices.DecryptString256Bit(user?.Realname) ?? "UnknownUser";

            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", userFolder);
            if (!Directory.Exists(tempDir))
                return new List<(string, long)>();

            var files = Directory.GetFiles(tempDir)
                .Select(f => (FileName: Path.GetFileName(f), FileSize: new FileInfo(f).Length))
                .ToList();

            return files;
        }

        // Removes a file from the user's temp folder
        public bool RemoveTempUserFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // Get user info
            var userId = _systemInfo.GetUserId().Result.Id;
            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if (user == null)
                return false;

            var decryptedRealName = _encryptionServices.DecryptString256Bit(user.Realname ?? "UnknownUser");
            var userName = user.UserName ?? "UnknownUser";

            // Build the full path: wwwroot/Attachments/{decryptedRealName}/{userName}/{fileName}
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", decryptedRealName);
            var filePath = Path.Combine(wwwrootPath, fileName);

            // Optional: Prevent directory traversal
            if (fileName.Contains("..") || Path.IsPathRooted(fileName))
                return false;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

        // Merges two PDF files and returns the merged file as an encrypted and compressed byte array
        public async Task<(byte[]? MergedFile, string? Error)> MergeTwoPdfFilesAsync(MergePdfViewForm requestDTO)
        {
            if (string.IsNullOrWhiteSpace(requestDTO.OriginalFilePath) || requestDTO.File == null)
                return (null, "Original file path and the file to merge must be provided.");

            // Validate file is PDF
            if (!Path.GetExtension(requestDTO.File.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                return (null, "The uploaded file must be a PDF document.");

            // Create unique temp file paths with clear naming
            var tempId = Guid.NewGuid().ToString("N");
            string tempDir = Path.GetTempPath();
            string uploadedFilePath = Path.Combine(tempDir, $"uploaded_{tempId}.pdf");
            string originalDecryptedPath = Path.Combine(tempDir, $"original_{tempId}.pdf");
            string mergedPdfPath = Path.Combine(tempDir, $"merged_{tempId}.pdf");
            string finalEncryptedPath = Path.Combine(tempDir, $"final_{tempId}.gz");

            var tempFiles = new List<string> { uploadedFilePath, originalDecryptedPath, mergedPdfPath, finalEncryptedPath };

            try
            {
                // 1. Locate and validate the original file
                string originalPath = ResolveFilePath(requestDTO.OriginalFilePath);
                if (originalPath == null)
                    return (null, $"Original file not found at: {requestDTO.OriginalFilePath}");

                bool isCompressed = originalPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);

                // 2. Save the uploaded file
                using (var fileStream = new FileStream(uploadedFilePath, FileMode.Create))
                    await requestDTO.File.CopyToAsync(fileStream);

                // 3. Process the original file (decrypt/decompress if needed)
                if (!await ProcessOriginalPdfFileAsync(originalPath, originalDecryptedPath))
                    return (null, "Could not process the original file. It may not be a valid PDF.");

                // 4. Merge the PDFs using PdfSharp
                using (var mergedPdfDoc = new PdfSharp.Pdf.PdfDocument())
                {
                    // Add pages from original file
                    try
                    {
                        using var originalDoc = PdfSharp.Pdf.IO.PdfReader.Open(
                            originalDecryptedPath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                        for (int i = 0; i < originalDoc.PageCount; i++)
                            mergedPdfDoc.AddPage(originalDoc.Pages[i]);
                    }
                    catch (Exception ex)
                    {
                        return (null, $"Error reading original PDF: {ex.Message}");
                    }

                    // Add pages from uploaded file
                    try
                    {
                        using var uploadedDoc = PdfSharp.Pdf.IO.PdfReader.Open(
                            uploadedFilePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                        for (int i = 0; i < uploadedDoc.PageCount; i++)
                            mergedPdfDoc.AddPage(uploadedDoc.Pages[i]);
                    }
                    catch (Exception ex)
                    {
                        return (null, $"Error reading uploaded PDF: {ex.Message}");
                    }

                    // Save the merged document
                    mergedPdfDoc.Save(mergedPdfPath);
                }

                // 5. Verify the merged PDF was created successfully
                if (!File.Exists(mergedPdfPath) || new FileInfo(mergedPdfPath).Length == 0)
                    return (null, "Failed to create merged PDF file.");

                // 6. Read encryption keys
                byte[] key = Convert.FromBase64String(_configuration["FileEncrypt:key"]);
                byte[] iv = Convert.FromBase64String(_configuration["FileEncrypt:iv"]);

                // 7. Compress and encrypt the merged file
                using (var inputFileStream = new FileStream(mergedPdfPath, FileMode.Open, FileAccess.Read))
                using (var compressedStream = new MemoryStream())
                {
                    // Compress the merged PDF
                    await CompressToGZipAsync(inputFileStream, compressedStream);
                    compressedStream.Position = 0;

                    // Encrypt the compressed data
                    using (var outputFileStream = new FileStream(finalEncryptedPath, FileMode.Create))
                    {
                        await outputFileStream.WriteAsync(iv, 0, iv.Length);
                        using var aes = Aes.Create();
                        aes.Key = key;
                        aes.IV = iv;
                        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                        using var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write);
                        await compressedStream.CopyToAsync(cryptoStream);
                    }
                }

                // 8. Save the merged file back to the original location (replace the original)
                byte[] mergedFileBytes = await File.ReadAllBytesAsync(finalEncryptedPath);
                await File.WriteAllBytesAsync(originalPath, mergedFileBytes);

                // 9. Return the merged file bytes for potential preview or download
                return (mergedFileBytes, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error merging PDF files: {ex.Message}");
            }
            finally
            {
                // Clean up temp files
                foreach (string path in tempFiles)
                {
                    try { if (File.Exists(path)) File.Delete(path); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }


        // Optimized path resolution with minimal I/O
        private string? ResolveFilePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Normalize path separators in one operation
            path = path.Replace('/', Path.DirectorySeparatorChar);

            // Check direct path first to minimize I/O
            if (File.Exists(path)) return path;

            // Try absolute path
            try
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath)) return fullPath;
            }
            catch { /* Ignore path resolution errors */ }

            // Try relative to current directory
            string relativePath = Path.Combine(Directory.GetCurrentDirectory(), path);
            if (File.Exists(relativePath)) return relativePath;

            return null;
        }
        private async Task<bool> ProcessOriginalPdfFileAsync(string originalPath, string outputPath)
        {
            try
            {
                // Check if file might be encrypted/compressed
                using (var headerStream = new FileStream(originalPath, FileMode.Open, FileAccess.Read))
                {
                    if (headerStream.Length < 4)
                        return false;

                    byte[] header = new byte[4];
                    await headerStream.ReadAsync(header, 0, 4);
                    string headerStr = Encoding.ASCII.GetString(header);

                    // If it's already a PDF, just copy it
                    if (headerStr == "%PDF")
                    {
                        File.Copy(originalPath, outputPath, true);
                        return true;
                    }
                }

                // Try to decrypt the file
                byte[] key = Convert.FromBase64String(_configuration["FileEncrypt:key"]);
                byte[] iv = new byte[16];

                using (var inputStream = new FileStream(originalPath, FileMode.Open, FileAccess.Read))
                {
                    // Read the IV
                    await inputStream.ReadAsync(iv, 0, iv.Length);

                    // Create decryption objects
                    using var aes = Aes.Create();
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // Create decryptor
                    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);

                    // Try to decompress if it's a GZip file
                    if (originalPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    {
                        using var gzipStream = new System.IO.Compression.GZipStream(
                            cryptoStream, System.IO.Compression.CompressionMode.Decompress);
                        using var outputStream = new FileStream(outputPath, FileMode.Create);
                        await gzipStream.CopyToAsync(outputStream);
                    }
                    else
                    {
                        // Just decrypt without decompression
                        using var outputStream = new FileStream(outputPath, FileMode.Create);
                        await cryptoStream.CopyToAsync(outputStream);
                    }
                }

                // Verify the output is a valid PDF
                using (var verifyStream = new FileStream(outputPath, FileMode.Open, FileAccess.Read))
                {
                    byte[] pdfHeader = new byte[4];
                    if (verifyStream.Length < 4)
                        return false;

                    await verifyStream.ReadAsync(pdfHeader, 0, 4);
                    return Encoding.ASCII.GetString(pdfHeader) == "%PDF";
                }
            }
            catch
            {
                // If all else fails, try a direct copy as a last resort
                try
                {
                    File.Copy(originalPath, outputPath, true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Handle original PDF preparation efficiently
        private async Task<bool> PrepareOriginalPdfAsync(string originalPath, string outputPath)
        {
            try
            {
                // Check if encrypted by reading header only
                bool isEncrypted;
                using (var fs = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096))
                {
                    if (fs.Length < 4) return false;

                    byte[] header = new byte[4];
                    await fs.ReadAsync(header, 0, 4);
                    isEncrypted = Encoding.ASCII.GetString(header) != "%PDF";
                }

                if (!isEncrypted)
                {
                    // Direct copy for unencrypted files
                    File.Copy(originalPath, outputPath, true);
                    return true;
                }

                // Handle encrypted file
                byte[] key = Convert.FromBase64String(_configuration["FileEncrypt:key"] ?? string.Empty);
                if (key.Length == 0) return false;

                using var decryptedStream = await DecryptFileAsync(originalPath, key);
                if (decryptedStream == null) return false;

                // Verify PDF header after decryption
                decryptedStream.Position = 0;
                byte[] pdfHeader = new byte[4];
                await decryptedStream.ReadAsync(pdfHeader, 0, 4);

                if (Encoding.ASCII.GetString(pdfHeader) == "%PDF")
                {
                    // Save decrypted PDF
                    decryptedStream.Position = 0;
                    using var outputStream = new FileStream(outputPath, FileMode.Create);
                    await decryptedStream.CopyToAsync(outputStream);
                    return true;
                }

                // Try decompression if not a valid PDF after decryption
                decryptedStream.Position = 0;
                using var decompressedStream = await TryDecompressGZipAsync(decryptedStream);
                if (decompressedStream == null) return false;

                // Check decompressed content for PDF header
                decompressedStream.Position = 0;
                await decompressedStream.ReadAsync(pdfHeader, 0, 4);
                if (Encoding.ASCII.GetString(pdfHeader) != "%PDF") return false;

                // Save decompressed PDF
                decompressedStream.Position = 0;
                using var finalOutputStream = new FileStream(outputPath, FileMode.Create);
                await decompressedStream.CopyToAsync(finalOutputStream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Try to decompress a stream, return null if fails
        private async Task<MemoryStream?> TryDecompressGZipAsync(Stream inputStream)
        {
            try
            {
                var outputStream = new MemoryStream();
                using var gzipStream = new System.IO.Compression.GZipStream(
                    inputStream, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);
                await gzipStream.CopyToAsync(outputStream);
                outputStream.Position = 0;
                return outputStream;
            }
            catch
            {
                return null;
            }
        }

        // Merge PDFs efficiently
        private async Task<bool> MergePdfsAsync(string file1Path, string file2Path, string outputPath)
        {
            try
            {
                using var outputDoc = new PdfSharp.Pdf.PdfDocument();

                // Process files in sequence
                foreach (string path in new[] { file1Path, file2Path })
                {
                    using var inputDoc = PdfSharp.Pdf.IO.PdfReader.Open(
                        path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);

                    for (int i = 0; i < inputDoc.PageCount; i++)
                        outputDoc.AddPage(inputDoc.Pages[i]);
                }

                outputDoc.Save(outputPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Encrypt and compress efficiently
        private async Task<byte[]?> EncryptAndCompressPdfAsync(string inputPath, string tempOutputPath)
        {
            try
            {
                var keyStr = _configuration["FileEncrypt:key"];
                var ivStr = _configuration["FileEncrypt:iv"];
                if (string.IsNullOrEmpty(keyStr) || string.IsNullOrEmpty(ivStr))
                    return null;

                byte[] keyBytes = Convert.FromBase64String(keyStr);
                byte[] ivBytes = Convert.FromBase64String(ivStr);

                // Compress and encrypt in memory where possible
                using var inputFileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920);
                using var compressedStream = new MemoryStream((int)(inputFileStream.Length * 0.8)); // Estimate compressed size

                // Compress
                await CompressToGZipAsync(inputFileStream, compressedStream);
                compressedStream.Position = 0;

                // Encrypt to file
                await EncryptStreamToFileAsync(compressedStream, tempOutputPath, keyBytes, ivBytes);

                // Read back as bytes directly
                return await File.ReadAllBytesAsync(tempOutputPath);
            }
            catch
            {
                return null;
            }
        }
        //this method to merge docx files and return the merged file as bytes
        public async Task<(byte[]? MergedFile, string? FileName, string? Error)> MergeDocxFilesAsync(List<IFormFile> files)
        {
            if (files == null || files.Count < 2)
                return (null, null, "At least two .docx files are required.");

            var tempPaths = new List<string>();
            try
            {
                // Save uploaded files to temp
                foreach (var file in files)
                {
                    if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".docx")
                        return (null, null, "All files must be .docx format.");

                    var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
                    using (var stream = new FileStream(tempPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    tempPaths.Add(tempPath);
                }

                // Use the directory of the first temp file as the origin path
                var originDir = Path.GetDirectoryName(tempPaths[0])!;
                var mergedFileName = $"merged_{Guid.NewGuid()}.docx";
                var outputFile = Path.Combine(originDir, mergedFileName);

                // Merge using your existing logic
                File.Copy(tempPaths[0], outputFile, true);
                using (WordprocessingDocument mainDoc = WordprocessingDocument.Open(outputFile, true))
                {
                    var mainBody = mainDoc.MainDocumentPart.Document.Body;
                    for (int i = 1; i < tempPaths.Count; i++)
                    {
                        using (WordprocessingDocument tempDoc = WordprocessingDocument.Open(tempPaths[i], true))
                        {
                            foreach (var element in tempDoc.MainDocumentPart.Document.Body.Elements())
                            {
                                mainBody.Append(element.CloneNode(true));
                            }
                        }
                    }
                    mainDoc.MainDocumentPart.Document.Save();
                }

                // Return the merged file as bytes and its file name
                var mergedBytes = await File.ReadAllBytesAsync(outputFile);
                return (mergedBytes, mergedFileName, null);
            }
            catch (Exception ex)
            {
                return (null, null, $"Error merging Word documents: {ex.Message}");
            }
            finally
            {
                foreach (var path in tempPaths)
                    if (File.Exists(path)) File.Delete(path);
                // Optionally, delete the merged file after reading
                // if (File.Exists(outputFile)) File.Delete(outputFile);
            }
        }
        // Method to get a decrypted file by its path
        public async Task<(Stream? fileStream, string? contentType, string? error)> GetDecryptedFileStreamAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return (null, null, "filePath is required.");

            try
            {
                // Normalize and validate path
                filePath = filePath.Replace('/', '\\').Trim();

                try { filePath = Path.GetFullPath(filePath); }
                catch (Exception ex) { return (null, null, $"Invalid file path: {ex.Message}"); }

                if (!File.Exists(filePath))
                    return (null, null, $"File not found: {filePath}");

                // Get content info
                var contentType = GetContentType(filePath);
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                bool isCompressed = extension == ".gz";

                // Special handling for ZIP files - don't decrypt
                if (extension == ".zip")
                    return (new FileStream(filePath, FileMode.Open, FileAccess.Read), "application/zip", null);

                // Try AES decryption
                try
                {
                    byte[] key = Convert.FromBase64String(_configuration["FileEncrypt:key"]);
                    var memoryStream = new MemoryStream();

                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] iv = new byte[16];
                        await fileStream.ReadAsync(iv, 0, iv.Length);

                        using var aes = Aes.Create();
                        aes.Key = key;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                        using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);

                        if (isCompressed)
                        {
                            using var gzipStream = new System.IO.Compression.GZipStream(
                                cryptoStream, System.IO.Compression.CompressionMode.Decompress);
                            await gzipStream.CopyToAsync(memoryStream);
                        }
                        else
                        {
                            await cryptoStream.CopyToAsync(memoryStream);
                        }
                    }

                    memoryStream.Position = 0;
                    return (memoryStream, contentType, null);
                }
                catch
                {
                    // Fallback - try to handle as compressed or plain file
                    if (isCompressed)
                    {
                        try
                        {
                            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                            var memoryStream = new MemoryStream();

                            using var gzipStream = new System.IO.Compression.GZipStream(
                                fileStream, System.IO.Compression.CompressionMode.Decompress);
                            await gzipStream.CopyToAsync(memoryStream);

                            memoryStream.Position = 0;
                            return (memoryStream, contentType, null);
                        }
                        catch
                        {
                            // If decompression fails, return as-is
                        }
                    }

                    // Return file as-is
                    return (new FileStream(filePath, FileMode.Open, FileAccess.Read), contentType, null);
                }
            }
            catch (Exception ex)
            {
                return (null, null, $"Error processing file: {ex.Message}");
            }
        }


        // Helper to check if file is GZip
        private static bool IsGZipFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".gz", StringComparison.OrdinalIgnoreCase);
        }

        // Helper to get content type (you can move this to a utility class if needed)
        private string GetContentType(string path)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }


        //upload multiy pdf
        public async Task<(List<(string filePath, string fileType, string? notice)> files, string? error)> uploadWithType(MultiFileFormViewForm filesForm)
        {
            var userId = (await _systemInfo.GetUserId()).Id;
            if (string.IsNullOrEmpty(userId))
                return (null, "User ID is not available.");

            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if (user == null)
                return (null, "User not found.");

            var fileList = new List<(string filePath, string fileType, string? notice)>();
            var userFolder = _encryptionServices.DecryptString256Bit(user.Realname);
            var wwwrootDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", userFolder);
            if (!Directory.Exists(wwwrootDir))
                Directory.CreateDirectory(wwwrootDir);

            foreach (var file in filesForm.Files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var isPdf = extension == ".pdf" && file.ContentType == "application/pdf";
                // Accept common Word MIME types and fallback to extension check
                var isWord = (extension == ".docx" &&
                                (file.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                                 file.ContentType == "application/octet-stream" ||
                                 file.ContentType == "application/zip")) ||
                             (extension == ".doc" &&
                                (file.ContentType == "application/msword" ||
                                 file.ContentType == "application/octet-stream"));

                string fileType;
                string? notice = null;

                if (isPdf)
                {
                    fileType = "pdf";
                }
                else if (isWord)
                {
                    fileType = "word";
                    notice = "Word file uploaded";
                }
                else
                {
                    return (null, $"File '{file.FileName}' is not a PDF or Word document.");
                }

                // Sanitize and uniquify file name
                var safeFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{extension}";
                var newFilePath = Path.Combine(wwwrootDir, safeFileName);

                await using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = Path.Combine("Attachments", userFolder, safeFileName).Replace("\\", "/");
                fileList.Add((relativePath, fileType, notice));
            }
            return (fileList, null);
        }

        public async Task<(Stream? fileStream, string? contentType, string? error)> GetFileAsync(string relativePath)
        {
            var userId = (await _systemInfo.GetUserId()).Id;
            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);


            try
            {
                // Convert relative path to full path
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", $"{_encryptionServices.DecryptString256Bit(user.Realname)
                    }", relativePath);

                if (!System.IO.File.Exists(fullPath))
                {
                    return (null, null, "File not found.");
                }

                // Get content type
                var contentType = GetContentType(fullPath);

                // Create file stream
                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                return (fileStream, contentType, null);
            }
            catch (Exception ex)
            {
                return (null, null, $"Error reading file: {ex.Message}");
            }
        }

        public async Task<(IActionResult, string? error)> DownloadPdf(MultiFileFormViewForm filesForm)
        {
            try
            {
                Guid Id = Guid.NewGuid();
                var fileName = $"{Id}.pdf";

                var pdfDierctoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdf"); // output directory

                if (!System.IO.File.Exists(pdfDierctoryPath))
                {
                    Directory.CreateDirectory(pdfDierctoryPath);
                }
                var path = Path.Combine(pdfDierctoryPath, fileName);
                var browserFetcherOptions = new BrowserFetcherOptions();
                var browserFetcher = new BrowserFetcher(browserFetcherOptions);
                await browserFetcher.DownloadAsync();
                using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true }))
                using (var page = await browser.NewPageAsync())
                {
                    var xForm = await GetHtml(filesForm);
                    await page.SetContentAsync(xForm);
                    await page.PdfAsync(path);
                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(path);
                    await using var stream = new FileStream(path, FileMode.Create);
                    stream.Write(pdfBytes);
                    var filePath = Path.Combine("pdf", fileName);
                    return (null, filePath);
                }
            }
            catch (Exception ex)
            {
                return (new BadRequestObjectResult(ex.Message), ex.Message);
            }
        }
        public void MergeDocxFiles(string[] sourceFiles, string outputFile)
        {
            File.Copy(sourceFiles[0], outputFile, true);
            using (WordprocessingDocument mainDoc = WordprocessingDocument.Open(outputFile, true))
            {
                var mainBody = mainDoc.MainDocumentPart.Document.Body;
                for (int i = 1; i < sourceFiles.Length; i++)
                {
                    using (WordprocessingDocument tempDoc = WordprocessingDocument.Open(sourceFiles[i], true))
                    {
                        foreach (var element in tempDoc.MainDocumentPart.Document.Body.Elements())
                        {
                            mainBody.Append(element.CloneNode(true));
                        }
                    }
                }
                mainDoc.MainDocumentPart.Document.Save();
            }
        }
        public async Task<string> GetHtml(MultiFileFormViewForm filesForm)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Forms", "documents_form.html");
            string htmlCode = await System.IO.File.ReadAllTextAsync(path);
            var base64Files = new List<string>();
            foreach (var file in filesForm.Files)
            {
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    base64Files.Add(Convert.ToBase64String(fileBytes));
                }
            }
            htmlCode.Replace("hello", "world");
            for (int i = 0; i < base64Files.Count; i++)
            {
                htmlCode.Replace($"document_image_{i}", base64Files[i]);
            }
            return (htmlCode);
        }
        //save system icons to wwwroot 

        public async Task<(string? file, string? error)> SaveToWwwrootAsync(FileViewForm fileForm)
        {

            var file = fileForm.File;
            if (file == null || file.Length == 0)
            {
                return (null, "No file was provided or the file is empty.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Save to wwwroot/Attachments (ensure this folder exists in your project)
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments");
            if (!Directory.Exists(wwwrootPath))
            {
                Directory.CreateDirectory(wwwrootPath);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(wwwrootPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative path for web access
            var relativePath = Path.Combine("Attachments", fileName).Replace("\\", "/");
            return (relativePath, null);
        }

        // Method to decrypt a file using AES encryption
        private async Task<MemoryStream> DecryptFileAsync(string filePath, byte[] key)
        {
            byte[] iv = new byte[16];
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await fileStream.ReadAsync(iv, 0, iv.Length);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);
            var memoryStream = new MemoryStream();
            await cryptoStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        // Method to compress a stream to GZip format
        private async Task CompressToStreamAsync(Stream input, Stream output)
        {
            using (var gzipStream = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            {
                await input.CopyToAsync(gzipStream);
            }
        }
        //Method To DeCompress a stream To GZip Format
        private static async Task<MemoryStream> DecompressGZipAsync(Stream compressedStream)
        {
            var decompressedStream = new MemoryStream();
            using (var gzip = new System.IO.Compression.GZipStream(compressedStream, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true))
            {
                await gzip.CopyToAsync(decompressedStream);
            }
            decompressedStream.Position = 0;
            return decompressedStream;
        }

        // Compress a stream to GZip format
        private async Task CompressToGZipAsync(Stream input, Stream output)
        {
            using (var gzipStream = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            {
                await input.CopyToAsync(gzipStream);
            }
        }

        // Encrypt a stream and write to a file using AES
        private async Task EncryptStreamToFileAsync(Stream input, string filePath, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var outFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await outFileStream.WriteAsync(aes.IV, 0, aes.IV.Length);
                using var cryptoStream = new CryptoStream(outFileStream, encryptor, CryptoStreamMode.Write);
                await input.CopyToAsync(cryptoStream);
            }
        }
    }
}
