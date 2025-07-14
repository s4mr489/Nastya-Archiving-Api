using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs.file;
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
        public FileServices(AppDbContext context,
                            IHttpContextAccessor httpcontext, 
                            ISystemInfoServices systeminfo, 
                            IConfiguration configuration) : base(null, context)
        {
            _context = context;
            _httpContext = httpcontext;
            _systemInfo = systeminfo;
            _configuration = configuration;
        }

        //upload single pdf file and encrypt it using AES encryption
        public async Task<(string? file, long fileSize, string? error)> upload(FileViewForm fileForm)
        {
            var userId = (await _systemInfo.GetUserId()).Id;
            if (string.IsNullOrEmpty(userId))
            {
                return (null, 0, "User ID is not available.");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if(user == null)
            {
                return (null, 0, "User not found.");
            }
            var group = _context.Usersgroups.FirstOrDefault(g => g.Id == user.GroupId);
            var depr = _context.GpDepartments.FirstOrDefault(d => d.Id == user.DepariId);

            var file = fileForm.File;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isPdf = extension == ".pdf" && file.ContentType == "application/pdf";
            if (!isPdf)
            {
                return (null, 0, $"File '{file.FileName}' is not a PDF.");
            }

            long fileSize = file.Length; // Get the file size in bytes


            string attachmentsDir = Path.Combine(
                "Adminst" ,
                DateTime.Now.Year.ToString(),
                depr.Dscrp,
                DateTime.Now.Month.ToString(),
                group.Groupdscrp
            );

            if (!Directory.Exists(attachmentsDir))
            {
                Directory.CreateDirectory(attachmentsDir);
            }

            var fileName = $"{await _systemInfo.GetLastRefNo()}-{Guid.NewGuid()}{extension}.gz";
            var filePath = Path.Combine(attachmentsDir, fileName);

            byte[] key = Convert.FromBase64String(_configuration["FileEncrypt:key"]);
            byte[] iv = Convert.FromBase64String(_configuration["FileEncrypt:iv"]);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var outFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await outFileStream.WriteAsync(aes.IV, 0, aes.IV.Length);
                    using (var cryptoStream = new CryptoStream(outFileStream, encryptor, CryptoStreamMode.Write))
                    using (var inputStream = file.OpenReadStream())
                    {
                        await CompressToStreamAsync(inputStream, cryptoStream);
                    }
                }
            }

            var webPath = Path.Combine(
                DateTime.Now.Year.ToString(),
                depr.Dscrp,
                DateTime.Now.Month.ToString(),
                group.Groupdscrp,
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

        public async Task<List<string>> GetTempFolderFilesAsync()
        {
            var userId = (await _systemInfo.GetUserId()).Id;
            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            var userFolder = user?.Realname ?? "UnknownUser";

            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", userFolder);
            if (!Directory.Exists(tempDir))
                return new List<string>();

            var files = Directory.GetFiles(tempDir)
                .Select(f => Path.GetFileName(f))
                .ToList();

            return files;
        }

        // Removes a file from the user's temp folder
        public bool RemoveTempUserFile(string fileName)
        {
            var user = _systemInfo.GetUserId().Result.Id;
            var realName = _context.Users.FirstOrDefault(u => u.Id.ToString() == user)?.Realname ?? "UnknownUser";

            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            //var fullPath = $"C:/Users/gcc/source/repos/archiving_system_api/archiving_system_api/wwwroot/{fileName}";
            var fullPath = $"{Directory.GetCurrentDirectory()}/wwwroot/{fileName}";
            var safeRealName = string.Concat(realName.Split(Path.GetInvalidFileNameChars()));
            var tempDir = Path.Combine(Path.GetTempPath(), "ArchivingTempFiles", safeRealName);
            var filePath = Path.Combine(tempDir, fullPath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

        //merged two pdf files and return the merged file as byte array
        public async Task<(byte[]? MergedFile, string? Error)> MergeTwoPdfFilesAsync(MergePdfViewForm requestDTO)
        {
            if (string.IsNullOrWhiteSpace(requestDTO.OriginalFilePath) || requestDTO.File == null)
                return (null, "Original file path and the file to merge must be provided.");

            string file2Path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
            string tempDecryptedOriginalPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_decrypted.pdf");
            string tempMergedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_merged.pdf");

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
                    File.Copy(requestDTO.OriginalFilePath, tempDecryptedOriginalPath, true);

                // Merge and save to tempMergedPath
                using (var outputDoc = new PdfSharp.Pdf.PdfDocument())
                {
                    foreach (var path in new[] { originalToMergePath, file2Path })
                        using (var inputDoc = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
                            for (int i = 0; i < inputDoc.PageCount; i++)
                                outputDoc.AddPage(inputDoc.Pages[i]);

                    outputDoc.Save(tempMergedPath);
                }

                // Encrypt the merged PDF and overwrite the original file
                var keyStr = _configuration["FileEncrypt:key"];
                var ivStr = _configuration["FileEncrypt:iv"];
                if (string.IsNullOrEmpty(keyStr) || string.IsNullOrEmpty(ivStr))
                    return (null, "Encryption key or IV is not configured.");

                byte[] keyBytes = Convert.FromBase64String(keyStr);
                byte[] ivBytes = Convert.FromBase64String(ivStr);

                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = ivBytes;
                    using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    using var outFileStream = new FileStream(requestDTO.OriginalFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    // Write IV first
                    await outFileStream.WriteAsync(aes.IV, 0, aes.IV.Length);
                    using var cryptoStream = new CryptoStream(outFileStream, encryptor, CryptoStreamMode.Write);
                    var mergedBytes = await File.ReadAllBytesAsync(tempMergedPath);
                    await cryptoStream.WriteAsync(mergedBytes, 0, mergedBytes.Length);
                }

                // Return the encrypted merged file as bytes
                var encryptedMergedBytes = await File.ReadAllBytesAsync(requestDTO.OriginalFilePath);
                return (encryptedMergedBytes, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error merging PDFs: {ex.Message}");
            }
            finally
            {
                foreach (var path in new[] { file2Path, tempDecryptedOriginalPath, tempMergedPath })
                    if (File.Exists(path)) File.Delete(path);
            }
        }

        // Method to get a decrypted file by its path
        public async Task<(Stream? fileStream, string? fileName, string? contentType, string? error)> GetDecryptedFileByPathAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return (null, null, null, "fileName field is required.");
            }

            var rootDir = Environment.GetEnvironmentVariable("FILE_STORAGE_PATH");
            var fullPath = $"{rootDir}/{filePath}";

            if (!System.IO.File.Exists(fullPath))
            {
                return (null, null, null, "File not found.");
            }

            var keyString = _configuration["FileEncrypt:key"];
            if (string.IsNullOrEmpty(keyString))
                throw new InvalidOperationException("Encryption key is not configured. Please set FileEncrypt:key in appsettings.json.");

            var contentType = GetContentType(fullPath);
            byte[] key = Convert.FromBase64String(keyString);

            try
            {
                // Decrypt the file
                MemoryStream decryptedStream = await DecryptFileAsync(fullPath, key);
                decryptedStream.Position = 0;

                // If the file is compressed (ends with .gz), decompress it using the extension method
                if (Path.GetExtension(fullPath).Equals(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    var decompressedStream = await DecompressGZipAsync(decryptedStream);
                    decompressedStream.Position = 0;
                    return (decompressedStream, Path.GetFileNameWithoutExtension(filePath), contentType, null);
                }

                return (decryptedStream, Path.GetFileName(filePath), contentType, null);
            }
            catch (Exception ex)
            {
                return (null, null, null, $"Error reading or decrypting the file: {ex.Message}");
            }
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
        public async Task<(List<string> files, string? error)> upload(MultiFileFormViewForm filesForm)
        {
            var userId = (await _systemInfo.GetUserId()).Id;
            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID is not available.");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            var fileList = new List<string>();
            // Save to wwwroot/Attachments/{UserName}
            var wwwrootDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", $"{user.Realname}");
            if (!Directory.Exists(wwwrootDir))
                Directory.CreateDirectory(wwwrootDir);

            foreach (var file in filesForm.Files)
            {
                // Check for PDF by extension and content type
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var isPdf = extension == ".pdf" && file.ContentType == "application/pdf";
                if (!isPdf)
                {
                    return (null, $"File '{file.FileName}' is not a PDF.");
                }
                // Generate a unique file name to avoid collisions
                var newFileName = $"{file.FileName}";
                var newFilePath = Path.Combine(wwwrootDir, newFileName);

                await using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for web access
                var relativePath = Path.Combine("Attachments", $"{user.Realname}", newFileName).Replace("\\", "/");
                fileList.Add(relativePath);
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
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", $"{user.Realname}", relativePath);

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
    }
}
