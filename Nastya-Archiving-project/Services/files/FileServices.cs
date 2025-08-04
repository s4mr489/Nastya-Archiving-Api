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

            string file2Path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
            string tempDecryptedOriginalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_decrypted.pdf");
            string tempMergedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_merged.pdf");
            string tempEncryptedCompressedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_merged_encrypted.gz");

            try
            {
                // Save the uploaded file to temp
                await using (var s2 = new FileStream(file2Path, FileMode.Create))
                    await requestDTO.File.CopyToAsync(s2);

                // Check if the original file is encrypted (not a PDF header)
                bool isEncrypted;
                using (var fs = new FileStream(requestDTO.OriginalFilePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] header = new byte[4];
                    await fs.ReadAsync(header, 0, 4);
                    isEncrypted = Encoding.ASCII.GetString(header) != "%PDF";
                }

                // If encrypted, decrypt to tempDecryptedOriginalPath, else just copy
                string originalToMergePath = tempDecryptedOriginalPath;
                if (isEncrypted)
                {
                    var keyString = _configuration["FileEncrypt:key"];
                    if (string.IsNullOrEmpty(keyString))
                        return (null, "Encryption key is not configured.");

                    byte[] key = Convert.FromBase64String(keyString);
                    using var decryptedStream = await DecryptFileAsync(requestDTO.OriginalFilePath, key);
                    await File.WriteAllBytesAsync(tempDecryptedOriginalPath, decryptedStream.ToArray());
                }
                else
                {
                    File.Copy(requestDTO.OriginalFilePath, tempDecryptedOriginalPath, true);
                }

                // Merge and save to tempMergedPath
                using (var outputDoc = new PdfSharp.Pdf.PdfDocument())
                {
                    foreach (var path in new[] { originalToMergePath, file2Path })
                        using (var inputDoc = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                            for (int i = 0; i < inputDoc.PageCount; i++)
                                outputDoc.AddPage(inputDoc.Pages[i]);

                    outputDoc.Save(tempMergedPath);
                }

                // Encrypt and compress the merged PDF using external helpers
                var keyStr = _configuration["FileEncrypt:key"];
                var ivStr = _configuration["FileEncrypt:iv"];
                if (string.IsNullOrEmpty(keyStr) || string.IsNullOrEmpty(ivStr))
                    return (null, "Encryption key or IV is not configured.");

                byte[] keyBytes = Convert.FromBase64String(keyStr);
                byte[] ivBytes = Convert.FromBase64String(ivStr);

                // Use streams for compression and encryption
                await using (var mergedFileStream = new FileStream(tempMergedPath, FileMode.Open, FileAccess.Read))
                await using (var compressedStream = new MemoryStream())
                {
                    await CompressToGZipAsync(mergedFileStream, compressedStream);
                    compressedStream.Position = 0;
                    await EncryptStreamToFileAsync(compressedStream, tempEncryptedCompressedPath, keyBytes, ivBytes);
                }

                // Return the encrypted and compressed merged file as bytes
                var encryptedMergedBytes = await File.ReadAllBytesAsync(tempEncryptedCompressedPath);
                return (encryptedMergedBytes, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error merging PDFs: {ex.Message}");
            }
            finally
            {
                foreach (var path in new[] { file2Path, tempDecryptedOriginalPath, tempMergedPath, tempEncryptedCompressedPath })
                    if (File.Exists(path)) File.Delete(path);
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
        public async Task<(byte[]? fileBytes, string? fileName, string? contentType, string? error)> GetDecryptedFileByPathAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return (null, null, null, "filePath is required.");

            string? tempOutputPath = null;

            try
            {
                // Normalize slashes and handle potential non-ASCII characters
                filePath = filePath.Replace('/', '\\').Trim();

                try
                {
                    filePath = Path.GetFullPath(filePath);
                }
                catch (Exception ex)
                {
                    return (null, null, null, $"Invalid file path: {ex.Message}");
                }

                if (!File.Exists(filePath))
                    return (null, null, null, $"File not found: {filePath}");

                var contentType = GetContentType(filePath);
                string originalFileName = Path.GetFileName(filePath);
                string extension = Path.GetExtension(originalFileName).ToLowerInvariant();

                // Special handling for ZIP files - don't try to decrypt these
                if (extension == ".zip")
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    return (fileBytes, originalFileName, "application/zip", null);
                }

                bool isCompressed = extension == ".gz";
                string finalExtension = isCompressed
                    ? Path.GetExtension(Path.GetFileNameWithoutExtension(originalFileName))
                    : extension;

                if (string.IsNullOrEmpty(finalExtension))
                    finalExtension = ".pdf"; // Default extension

                // APPROACH 1: Use encryption service
                tempOutputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{finalExtension}");

                try
                {
                    string decryptResult = _encryptionServices.Decrypt(filePath, tempOutputPath);

                    if (decryptResult == "0") // Success
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(tempOutputPath);

                        // Clean up filename
                        string cleanFileName = Path.GetFileNameWithoutExtension(originalFileName);
                        if (isCompressed)
                            cleanFileName = Path.GetFileNameWithoutExtension(cleanFileName);

                        return (fileBytes, cleanFileName + finalExtension, contentType, null);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue to next approach
                    System.Diagnostics.Debug.WriteLine($"Encryption service decrypt failed: {ex.Message}");
                }

                // APPROACH 2: Direct AES decryption
                try
                {
                    byte[] key = Convert.FromBase64String(_configuration["FileEncrypt:key"]);

                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] iv = new byte[16];
                        await fileStream.ReadAsync(iv, 0, iv.Length);

                        using (var aes = Aes.Create())
                        {
                            aes.Key = key;
                            aes.IV = iv;
                            aes.Mode = CipherMode.CBC;
                            aes.Padding = PaddingMode.PKCS7;

                            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                            using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);
                            using var memoryStream = new MemoryStream();

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

                            // Clean up filename
                            string cleanFileName = Path.GetFileNameWithoutExtension(originalFileName);
                            if (isCompressed)
                                cleanFileName = Path.GetFileNameWithoutExtension(cleanFileName);

                            return (memoryStream.ToArray(), cleanFileName + finalExtension, contentType, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Direct AES decryption failed: {ex.Message}");

                    // APPROACH 3: Handle as compressed or regular file
                    try
                    {
                        if (isCompressed)
                        {
                            // Handle compressed file without decryption
                            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                            using var gzipStream = new System.IO.Compression.GZipStream(
                                fileStream, System.IO.Compression.CompressionMode.Decompress);
                            using var memoryStream = new MemoryStream();

                            await gzipStream.CopyToAsync(memoryStream);

                            string cleanFileName = Path.GetFileNameWithoutExtension(originalFileName);
                            cleanFileName = Path.GetFileNameWithoutExtension(cleanFileName);

                            return (memoryStream.ToArray(), cleanFileName + finalExtension, contentType, null);
                        }
                        else
                        {
                            // Return file as-is
                            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                            return (fileBytes, originalFileName, contentType, null);
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        return (null, null, null, $"Failed to process file: {fallbackEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                return (null, null, null, $"Error processing file: {ex.Message}");
            }
            finally
            {
                // Clean up temp files
                if (!string.IsNullOrEmpty(tempOutputPath) && File.Exists(tempOutputPath))
                {
                    try { File.Delete(tempOutputPath); } catch { /* ignore cleanup errors */ }
                }
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
