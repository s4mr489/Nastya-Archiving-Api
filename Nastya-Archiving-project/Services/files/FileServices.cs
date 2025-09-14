using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.SystemInfo;
using PuppeteerSharp;
using System.Buffers;
using System.IO.Compression;
using System.Runtime.CompilerServices;
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
        private readonly ISystemInfoServices _systemInfoServices;
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

            // Check file size against license limits
            var file = fileForm.File;
            if (file == null)
                return (null, 0, "No file was provided.");

            long fileSize = file.Length;

            // Get license storage limit in MB (convert GB to MB)
            var licenseLimit = await GetLicenseStorageLimitMB();
            if (licenseLimit <= 0)
                return (null, fileSize, "Unable to determine storage limit from license.");

            // Convert file size to MB for comparison (with ceiling to ensure we don't allow files that are slightly over limit)
            double fileSizeMB = Math.Ceiling(fileSize / (1024.0 * 1024.0));

            // Check if file exceeds the license limit
            if (fileSizeMB > licenseLimit)
                return (null, fileSize, $"  حجم الملف المراد رفعه هو : {fileSizeMB} , وهو يتجاوز الحد المسموح به : {licenseLimit}.");

            // Check if DocType is valid
            //var docType = await _context.ArcivDocDscrps.FirstOrDefaultAsync(d => d.Id == fileForm.DocTypeDscrption);
            //if (docType == null)
            //    return (null, 0, $"Document type with ID {fileForm.DocType} not found.");

            var depr = _context.GpDepartments.FirstOrDefault(d => d.Id == user.DepariId);

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

            var storePath = await _context.PArcivingPoints.FirstOrDefaultAsync(s => s.AccountUnitId == user.AccountUnitId && s.DepartId == user.DepariId);
            if (storePath == null)
                return (null, 0, "Storage path not configured for user's account unit and department.");

            // Ensure StorePath and StartWith are not null
            string storePathValue = storePath.StorePath ?? string.Empty;
            string startWithValue = storePath.StartWith ?? string.Empty;

            // Sanitize base path components - remove trailing colons and other invalid characters
            storePathValue = SanitizeBasePath(storePathValue);
            startWithValue = SanitizePathComponent(startWithValue);

            // Get and sanitize other path components
            string yearStr = DateTime.Now.Year.ToString();
            string departmentName = SanitizePathComponent(depr?.Dscrp ?? "Unknown");
            string monthStr = DateTime.Now.Month.ToString();
            
            // Use document type description instead of group description
            

            // Include StartWith in the physical path with docTypeName instead of groupName
            string attachmentsDir = Path.Combine(
                storePathValue,
                startWithValue,
                yearStr,
                departmentName,
                monthStr,
                fileForm.DocTypeDscrption  // Using document type instead of group description
            );

            if (!Directory.Exists(attachmentsDir))
                Directory.CreateDirectory(attachmentsDir);

            var fileName = $"{await _systemInfo.GetLastRefNo()}{extension}.gz";
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

            // Keep the web path consistent with physical path
            var webPath = Path.Combine(
                storePathValue.Replace(Path.DirectorySeparatorChar, '/'),
                startWithValue.Replace(Path.DirectorySeparatorChar, '/'),
                yearStr,
                departmentName,
                monthStr,
                fileForm.DocTypeDscrption,
                fileName
            ).Replace(Path.DirectorySeparatorChar, '/');

            return (webPath, fileSize, null);
        }

        // Helper method to sanitize base file path
        private string SanitizeBasePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // Remove trailing colon if it exists (common issue in paths)
            if (path.EndsWith(":"))
            {
                // Keep only the drive letter with colon (e.g., "D:") 
                // or remove the trailing colon if not at position 1
                if (path.Length > 2 && path[1] == ':')
                    path = path.Substring(0, 2);
                else
                    path = path.TrimEnd(':');
            }

            // Ensure path doesn't have invalid characters
            return path;
        }

        // Helper method to sanitize path components
        private string SanitizePathComponent(string component)
        {
            if (string.IsNullOrEmpty(component))
                return "Unknown";

            // Remove characters that are invalid for Windows paths
            char[] invalidChars = Path.GetInvalidPathChars();
            string result = new string(component.Where(c => !invalidChars.Contains(c)).ToArray());

            // Also remove other potentially problematic characters for paths
            result = result.Replace(":", "_")
                           .Replace("?", "_")
                           .Replace("*", "_")
                           .Replace("\"", "_")
                           .Replace("<", "_")
                           .Replace(">", "_")
                           .Replace("|", "_")
                           .Replace("\\", "_")
                           .Replace("/", "_");

            return string.IsNullOrEmpty(result) ? "Unknown" : result;
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

        // Paginated version of GetTempFolderFilesAsync
        public async Task<(List<(string FileName, long FileSize)> files, int totalCount, string? error)> GetTempFolderFilesPaginatedAsync(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                // Validate pagination parameters
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100; // Limit maximum page size to prevent excessive resource usage

                // Get user info
                var userId = (await _systemInfo.GetUserId()).Id;
                if (string.IsNullOrEmpty(userId))
                    return (new List<(string, long)>(), 0, "User ID is not available.");

                var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
                if (user == null)
                    return (new List<(string, long)>(), 0, "User not found.");

                var userFolder = _encryptionServices.DecryptString256Bit(user?.Realname) ?? "UnknownUser";
                var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", userFolder);
                
                if (!Directory.Exists(tempDir))
                    return (new List<(string, long)>(), 0, null); // No error, just empty directory

                // Get all files and their information
                var allFiles = Directory.GetFiles(tempDir)
                    .Select(f => (FileName: Path.GetFileName(f), FileSize: new FileInfo(f).Length))
                    .OrderByDescending(f => new FileInfo(Path.Combine(tempDir, f.FileName)).LastWriteTime) // Order by most recent first
                    .ToList();

                // Get total count for pagination metadata
                int totalCount = allFiles.Count;

                // Calculate pagination
                int skip = (pageNumber - 1) * pageSize;
                
                // Apply pagination
                var paginatedFiles = allFiles
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

                return (paginatedFiles, totalCount, null);
            }
            catch (Exception ex)
            {
                return (new List<(string, long)>(), 0, $"Error retrieving files: {ex.Message}");
            }
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

            // Check file size against license limits
            var licenseLimit = await GetLicenseStorageLimitMB();
            if (licenseLimit <= 0)
                return (null, "Unable to determine storage limit from license.");

            // Check the size of the uploaded file
            double uploadedFileSizeMB = Math.Ceiling(requestDTO.File.Length / (1024.0 * 1024.0));
            if (uploadedFileSizeMB > licenseLimit)
                return (null, $"File size ({uploadedFileSizeMB:N1} MB) exceeds the license limit of {licenseLimit} MB.");

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

                // Check the size of the original file
                if (File.Exists(originalPath))
                {
                    var originalFileInfo = new FileInfo(originalPath);
                    double originalFileSizeMB = Math.Ceiling(originalFileInfo.Length / (1024.0 * 1024.0));

                    // Estimate the final size (sum of both files with some overhead)
                    double estimatedFinalSizeMB = originalFileSizeMB + uploadedFileSizeMB;
                    if (estimatedFinalSizeMB > licenseLimit)
                        return (null, $"Estimated size of merged file ({estimatedFinalSizeMB:N1} MB) exceeds the license limit of {licenseLimit} MB.");
                }

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

                // Check size of merged file
                var mergedFileInfo = new FileInfo(mergedPdfPath);
                double mergedFileSizeMB = Math.Ceiling(mergedFileInfo.Length / (1024.0 * 1024.0));
                if (mergedFileSizeMB > licenseLimit)
                    return (null, $"Merged file size ({mergedFileSizeMB:N1} MB) exceeds the license limit of {licenseLimit} MB.");

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

            // Get license storage limit in MB
            var licenseLimit = await GetLicenseStorageLimitMB();
            if (licenseLimit <= 0)
                return (null, null, "Unable to determine storage limit from license.");

            // Calculate total size of all files
            long totalSize = files.Sum(f => f.Length);
            double totalSizeMB = Math.Ceiling(totalSize / (1024.0 * 1024.0));

            // Check if total size exceeds the license limit
            if (totalSizeMB > licenseLimit)
                return (null, null, $"Total file size ({totalSizeMB:N1} MB) exceeds the license limit of {licenseLimit} MB.");

            // Check individual files
            foreach (var file in files)
            {
                double fileSizeMB = Math.Ceiling(file.Length / (1024.0 * 1024.0));
                if (fileSizeMB > licenseLimit)
                    return (null, null, $"File '{file.FileName}' size ({fileSizeMB:N1} MB) exceeds the license limit of {licenseLimit} MB.");
            }

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

                // Check the size of the merged file
                var mergedFileInfo = new FileInfo(outputFile);
                double mergedFileSizeMB = Math.Ceiling(mergedFileInfo.Length / (1024.0 * 1024.0));
                if (mergedFileSizeMB > licenseLimit)
                    return (null, null, $"Merged file size ({mergedFileSizeMB:N1} MB) exceeds the license limit of {licenseLimit} MB.");

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

            // Get license storage limit in MB
            var licenseLimit = await GetLicenseStorageLimitMB();
            if (licenseLimit <= 0)
                return (null, "Unable to determine storage limit from license.");

            // Calculate total size of all files
            long totalSize = filesForm.Files.Sum(f => f.Length);
            double totalSizeMB = Math.Ceiling(totalSize / (1024.0 * 1024.0));

            // Check if total size exceeds the license limit
            if (totalSizeMB > licenseLimit)
                return (null, $"  حجم الملف المراد رفعه هو : {totalSizeMB} , وهو يتجاوز الحد المسموح به : {licenseLimit}.");

            // Also check individual files to make sure none exceed the limit
            foreach (var file in filesForm.Files)
            {
                double fileSizeMB = Math.Ceiling(file.Length / (1024.0 * 1024.0));
                if (fileSizeMB > licenseLimit)
                    return (null, $"File '{file.FileName}' size ({fileSizeMB:N1} MB) exceeds the license limit of {licenseLimit} MB.");
            }

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
            var userId = await _systemInfoServices.GetUserId();
            if (userId.Id == null)
                return (null, "403"); // Unauthorized
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(u => u.UserId.ToString() == userId.Id);
            if (userPermissions.AllowDownload == 0)
                return (null, "403"); // Forbidden
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

      
        /// <summary>
        /// Creates a file download response for decrypted files that can be downloaded by the client
        /// </summary>
        /// <param name="fileUrls">List of file URLs to process</param>
        /// <param name="outputFolderName">Base name for the ZIP file (default: "ArchiveFiles")</param>
        /// <returns>Result with download URL or error message</returns>
        public async Task<(string? downloadUrl, string? error)> CopyFilesToDesktopAsync(List<string> fileUrls, string outputFolderName = "ArchiveFiles")
        {
            try
            {
                // Check user permissions
                var userIdResult = await _systemInfo.GetUserId();
                if (userIdResult.Id == null || string.IsNullOrEmpty(userIdResult.Id))
                    return (null, "403"); // Unauthorized - User ID not available

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id.ToString() == userIdResult.Id);

                if (user == null)
                    return (null, "User not found.");
        
                // Check download permissions
                var userPermissions = await _context.UsersOptionPermissions
                    .FirstOrDefaultAsync(u => u.UserId.ToString() == userIdResult.Id);

                if (userPermissions == null || userPermissions.AllowDownload == 0)
                    return (null, "403"); // Forbidden - User doesn't have download permission

                if (fileUrls == null || fileUrls.Count == 0)
                    return (null, "No file URLs provided");

                // Create a unique timestamp for the file
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // Create temp directory for processing
                string tempDir = Path.Combine(Path.GetTempPath(), $"TempArchive_{timestamp}");

                // Create a directory in wwwroot/Downloads that can be accessed via URL
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string downloadsFolderPath = Path.Combine(webRootPath, "Downloads");

                // Ensure downloads folder exists
                if (!Directory.Exists(downloadsFolderPath))
                {
                    Directory.CreateDirectory(downloadsFolderPath);
                }

                // Create a safe filename with the user's name for better identification
                string userFolder = _encryptionServices.DecryptString256Bit(user?.Realname ?? "unknown");
                string safeUserName = new string(userFolder.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
                string zipFileName = $"{safeUserName}_{outputFolderName}_{timestamp}.zip";
                string zipFilePath = Path.Combine(downloadsFolderPath, zipFileName);

                // Create the temp directory for processing
                Directory.CreateDirectory(tempDir);

                // Get encryption key
                string? keyString = _configuration["FileEncrypt:key"];
                if (string.IsNullOrEmpty(keyString))
                    return (null, "Encryption key not found in configuration");

                byte[] key = Convert.FromBase64String(keyString);

                // Process files
                int successCount = 0;
                var failedFiles = new List<string>();

                foreach (string url in fileUrls)
                {
                    if (string.IsNullOrEmpty(url))
                    {
                        failedFiles.Add("Empty URL provided");
                        continue;
                    }

                    try
                    {
                        // Resolve file path
                        string? filePath = ResolveFilePath(url);
                        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                        {
                            failedFiles.Add($"{url} (File not found)");
                            continue;
                        }

                        // Get filename (without .gz extension if present)
                        string fileName = Path.GetFileName(filePath);
                        string extension = Path.GetExtension(fileName).ToLowerInvariant();
                        bool isGzipped = extension == ".gz";

                        if (isGzipped)
                        {
                            fileName = Path.GetFileNameWithoutExtension(fileName); // Remove .gz extension
                        }

                        // Create destination path in temp directory
                        string destinationPath = Path.Combine(tempDir, fileName);

                        // Handle duplicate filenames
                        if (File.Exists(destinationPath))
                        {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                            string fileExtension = Path.GetExtension(fileName);
                            string uniqueSuffix = DateTime.Now.Ticks.ToString().Substring(0, 8);
                            destinationPath = Path.Combine(tempDir, $"{fileNameWithoutExt}_{uniqueSuffix}{fileExtension}");
                        }

                        // Decrypt and compress the file
                        await DecryptAndProcessFile(filePath, destinationPath, key, isGzipped);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failedFiles.Add($"{url} ({ex.Message})");
                    }
                }

                // If no files were processed successfully, return error
                if (successCount == 0)
                {
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch { /* Ignore cleanup errors */ }

                    return (null, $"Failed to process any files. Errors: {string.Join(", ", failedFiles)}");
                }

                // Create ZIP archive
                try
                {
                    // Delete existing ZIP file if it exists
                    if (File.Exists(zipFilePath))
                    {
                        File.Delete(zipFilePath);
                    }

                    // Create the ZIP file
                    ZipFile.CreateFromDirectory(tempDir, zipFilePath);

                    // Clean up temp directory
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch { /* Ignore cleanup errors */ }

                    // Return the download URL (relative to website root)
                    string downloadUrl = $"Downloads/{zipFileName}";
                    
                    return (downloadUrl, null);
                }
                catch (Exception ex)
                {
                    // If ZIP creation fails, return error
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch { /* Ignore cleanup errors */ }

                    return (null, $"Failed to create ZIP archive: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return (null, $"Error processing files: {ex.Message}");
            }
        }

        // Helper method to decrypt and process a file
        private async Task DecryptAndProcessFile(string sourcePath, string destinationPath, byte[] key, bool isGzipped)
        {
            using (var fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            {
                using (var outputStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    // Check if file is already a known format
                    if (fileStream.Length >= 4)
                    {
                        byte[] header = new byte[4];
                        await fileStream.ReadAsync(header, 0, 4);
                        fileStream.Position = 0; // Reset position

                        // If it's a known file format, just copy it
                        if (IsKnownFileHeader(header))
                        {
                            await fileStream.CopyToAsync(outputStream);
                            return;
                        }
                    }

                    // Read the IV
                    byte[] iv = new byte[16];
                    await fileStream.ReadAsync(iv, 0, iv.Length);

                    using var aes = Aes.Create();
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);

                    if (isGzipped)
                    {
                        // Decrypt and decompress
                        using var gzipStream = new GZipStream(cryptoStream, CompressionMode.Decompress);
                        await gzipStream.CopyToAsync(outputStream);
                    }
                    else
                    {
                        // Just decrypt
                        await cryptoStream.CopyToAsync(outputStream);
                    }
                }
            }
        }

        // Helper method to check if a file header indicates a known file format
        private bool IsKnownFileHeader(byte[] header)
        {
            // Check for common file signatures
            if (header.Length >= 4)
            {
                // PDF: %PDF (25 50 44 46)
                if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
                    return true;

                // ZIP/DOCX/etc.: PK (50 4B 03 04)
                if (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
                    return true;

                // PNG: (89 50 4E 47)
                if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                    return true;

                // JPEG: (FF D8 FF)
                if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                    return true;

                // GIF: GIF8 (47 49 46 38)
                if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a file download response for decrypted files
        /// </summary>
        /// <param name="fileUrls">List of file URLs to process</param>
        /// <param name="archiveName">Base name for the ZIP file (default: "DecryptedFiles")</param>
        /// <returns>Byte array with file data and metadata, or error message</returns>
        public async Task<(byte[]? fileBytes, string fileName, string contentType, string? error)> DownloadDecryptedFiles(List<string> fileUrls, string archiveName = "DecryptedFiles")
        {
            try
            {
                // Check user permissions
                var userIdResult = await _systemInfo.GetUserId();
                if (userIdResult.Id == null || string.IsNullOrEmpty(userIdResult.Id))
                    return (null, string.Empty, string.Empty, "403"); // Unauthorized - User ID not available

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id.ToString() == userIdResult.Id);

                if (user == null)
                    return (null, string.Empty, string.Empty, "User not found.");

                // Check download permissions
                var userPermissions = await _context.UsersOptionPermissions
                    .FirstOrDefaultAsync(u => u.UserId.ToString() == userIdResult.Id);

                if (userPermissions == null || userPermissions.AllowDownload == 0)
                    return (null, string.Empty, string.Empty, "403"); // Forbidden - User doesn't have download permission

                if (fileUrls == null || fileUrls.Count == 0)
                    return (null, string.Empty, string.Empty, "No file URLs provided");

                // First create the temporary ZIP file using the existing method
                var (downloadUrl, error) = await CopyFilesToDesktopAsync(fileUrls, archiveName);
                
                if (!string.IsNullOrEmpty(error))
                    return (null, string.Empty, string.Empty, error);
                    
                if (string.IsNullOrEmpty(downloadUrl))
                    return (null, string.Empty, string.Empty, "Failed to create download package.");

                // Convert the relative URL to a full file path
                string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string zipFilePath = Path.Combine(webRootPath, downloadUrl);
                
                if (!File.Exists(zipFilePath))
                    return (null, string.Empty, string.Empty, "Generated file not found on server.");
                
                // Get the filename from the path
                string fileName = Path.GetFileName(zipFilePath);
                
                // Read the file as bytes
                byte[] fileBytes = await File.ReadAllBytesAsync(zipFilePath);
                
                // Clean up the file after reading it
                try
                {
                    // Use asynchronous file deletion after a short delay to ensure
                    // the file is completely read before deletion
                    _ = Task.Run(async () => {
                        await Task.Delay(5000); // 5 second delay
                        try {
                            if (File.Exists(zipFilePath))
                                File.Delete(zipFilePath);
                        }
                        catch {
                            // Ignore cleanup errors
                        }
                    });
                }
                catch 
                {
                    // Ignore cleanup errors
                }
                
                // Return the file bytes along with metadata
                return (fileBytes, fileName, "application/zip", null);
            }
            catch (Exception ex)
            {
                return (null, string.Empty, string.Empty, $"Error preparing file for download: {ex.Message}");
            }
        }

        // Add this helper method to read license storage limit
        private async Task<double> GetLicenseStorageLimitMB()
        {
            try
            {
                // Define path to license file
                string licensePath = Path.Combine("wwwroot", "licenses", "Limit.txt");
                if (!File.Exists(licensePath))
                    return 10; // If no license file found, use default 10 MB limit

                // Read the license file content
                string fileContent = await File.ReadAllTextAsync(licensePath);

                // Parse the license data to find currentStorageMB instead of MaxStorageGB
                double storageLimit = 0;
                string storageLimitEncrypted = null;

                using (StringReader reader = new StringReader(fileContent))
                {
                    string line;
                    string currentSection = null;

                    while ((line = reader.ReadLine()) != null)
                    {
                        // Skip empty lines
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // Check if this is a section header
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            currentSection = line.Trim('[', ']');
                            continue;
                        }

                        // Check for currentStorageMB entry
                        if (currentSection == "NASTYA-ARCHIVING-LICENSE" && line.StartsWith("currentStorageMB="))
                        {
                            storageLimitEncrypted = line.Substring("currentStorageMB=".Length).Trim();
                            break;
                        }
                    }
                }

                // Decrypt the value if found
                if (storageLimitEncrypted != null)
                {
                    try
                    {
                        string decryptedValue = _encryptionServices.DecryptString256Bit(storageLimitEncrypted);
                        if (double.TryParse(decryptedValue, out storageLimit))
                        {
                            // No conversion needed since we're already working in MB
                            // Force the maximum limit to be 10 MB
                            return Math.Min(storageLimit, 10);
                        }
                    }
                    catch
                    {
                        // If decryption fails, don't return a default value
                        return 0;
                    }
                }

                // Default if not found or couldn't parse - enforce 10 MB limit
                return 10;
            }
            catch (Exception)
            {
                // If any error occurs, don't return a default value
                return 0;
            }
        }


        /// <summary>
        /// Updates a file's path by changing only the document type segment in the path and moving the file
        /// </summary>
        /// <param name="oldFilePath">The current file path</param>
        /// <param name="newDocType">The new document type description to use in the path</param>
        /// <returns>Tuple containing the updated file path (if successful) and any error message</returns>
        public async Task<(string? updatedFilePath, string? error)> UpdateFilePathAsync(string oldFilePath, string newDocType)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(oldFilePath))
                    return (null, "Original file path is required.");

                if (string.IsNullOrWhiteSpace(newDocType))
                    return (null, "New document type is required.");

                // Sanitize the new document type
                newDocType = SanitizePathComponent(newDocType);

                // Normalize paths for processing
                string normalizedOldPath = oldFilePath.Replace('/', Path.DirectorySeparatorChar);

                // Resolve the full path to the existing file
                string? resolvedOldPath = ResolveFilePath(normalizedOldPath);
                if (resolvedOldPath == null || !File.Exists(resolvedOldPath))
                    return (null, $"Original file not found at: {oldFilePath}");

                // Get the directory and filename
                string oldDirectory = Path.GetDirectoryName(resolvedOldPath);
                string fileName = Path.GetFileName(resolvedOldPath);

                if (string.IsNullOrEmpty(oldDirectory))
                    return (null, "Could not determine directory from file path.");

                // Split path into segments
                string[] pathSegments = oldDirectory.Split(Path.DirectorySeparatorChar);

                // We need enough segments to have a valid path with a document type
                if (pathSegments.Length < 2)
                    return (null, "The file path structure is not valid for document type update.");

                // Assuming the document type is the last segment in the directory path
                // (right before the filename)
                int docTypeIndex = pathSegments.Length - 1;

                // Create a copy of the segments array and replace the document type
                string[] newPathSegments = (string[])pathSegments.Clone();
                newPathSegments[docTypeIndex] = newDocType;

                // Reconstruct the directory path with the new document type
                string newDirectory = string.Join(Path.DirectorySeparatorChar.ToString(), newPathSegments);

                // Create the full new path
                string newPath = Path.Combine(newDirectory, fileName);

                // Ensure the new directory exists
                if (!Directory.Exists(newDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(newDirectory);
                    }
                    catch (Exception ex)
                    {
                        return (null, $"Failed to create destination directory: {ex.Message}");
                    }
                }

                // Check if destination file already exists
                if (File.Exists(newPath))
                    return (null, $"A file already exists at the new location: {newPath}");

                try
                {
                    // First try to copy the file to ensure it succeeds
                    File.Copy(resolvedOldPath, newPath);

                    // Then delete the original
                    File.Delete(resolvedOldPath);
                }
                catch (Exception ex)
                {
                    return (null, $"File operation failed: {ex.Message}");
                }

                // Convert back to web-friendly path format for return
                string webPath = newPath.Replace(Path.DirectorySeparatorChar, '/');

                return (webPath, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error updating file path: {ex.Message}");
            }
        }
    }
}
