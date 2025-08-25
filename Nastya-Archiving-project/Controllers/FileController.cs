using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Models.DTOs.Reports;
using Nastya_Archiving_project.Services.files;

namespace Nastya_Archiving_project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,User")]
    public class FileController : ControllerBase
    {
        private readonly IFilesServices _fileServices;

        public FileController(IFilesServices fileServices)
        {
            _fileServices = fileServices;
        }

        [HttpPost("Upload-single-file-To-DB")]
        public async Task<IActionResult> Upload([FromForm]FileViewForm fileForm)
        {
            var (file, fileSize, error) = await _fileServices.upload(fileForm);
            if (error != null) return BadRequest(error);
            return Ok(new { file, fileSize });
        }

        [HttpPost("Upload-multyi-TempFile-WithType")]
        public async Task<IActionResult> UploadWithType([FromForm] MultiFileFormViewForm filesForm)
        {
            var (files, error) = await _fileServices.uploadWithType(filesForm);
            if (error != null)
                return BadRequest(error);
            return Ok(files);
        }

        //[HttpGet("temp-files")]
        //public async Task<IActionResult> GetTempFiles()
        //{
        //    var files = await _fileServices.GetTempFolderFilesAsync();
        //    // Return as a list of objects with FileName and FileSize properties
        //    var result = files.Select(f => new { FileName = f.FileName, FileSize = f.FileSize }).ToList();
        //    return Ok(result);
        //}
        
        [HttpGet("temp-files/paginated")]
        public async Task<IActionResult> GetTempFilesPaginated([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var (files, totalCount, error) = await _fileServices.GetTempFolderFilesPaginatedAsync(pageNumber, pageSize);
            
            if (error != null)
                return BadRequest(error);
                
            // Return as a BaseResponseDTOs object with pagination metadata
            var result = new
            {
                Data = files.Select(f => new { FileName = f.FileName, FileSize = f.FileSize }).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
            
            return Ok(result);
        }

        [HttpDelete("temp-files/{fileName}")]
        public IActionResult RemoveTempFile(string fileName)
        {
            var result = _fileServices.RemoveTempUserFile(fileName);
            if (!result) return NotFound("File not found or could not be deleted.");
            return Ok("File deleted.");
        }

        [HttpPost("merge-pdf")]
        public async Task<IActionResult> MergePdf([FromForm] MergePdfViewForm form)
        {
            var (mergedFile, error) = await _fileServices.MergeTwoPdfFilesAsync(form);
            if (error != null) return BadRequest(error);
            return File(mergedFile, "application/pdf");
        }

        [HttpPut("merge-word")]
        public async Task<IActionResult> MergeDocx([FromForm] List<IFormFile> files)
        {
            var (mergedFile, fileName, error) = await _fileServices.MergeDocxFilesAsync(files);
            if (mergedFile == null)
                return BadRequest(error);

            return File(mergedFile, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName ?? "merged.docx");
        }


        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery] string relativePath)
        {
            var (fileStream, contentType, error) = await _fileServices.GetFileAsync(relativePath);
            if (error != null || fileStream == null) return NotFound(error);
            return File(fileStream, contentType ?? "application/octet-stream");
        }
            
        [HttpGet("GetDecrypted-by-Path")]
        public async Task<IActionResult> GetDecrypted([FromQuery] string filePath)
        {
            var (fileStream, contentType, error) = await _fileServices.GetDecryptedFileStreamAsync(filePath);
            if (error != null || fileStream == null) return NotFound(error);
            return File(fileStream, contentType ?? "application/octet-stream");
        }

        [HttpPost("save-wwwroot")]
        public async Task<IActionResult> SaveToWwwroot([FromForm] FileViewForm fileForm)
        {
            var (file, error) = await _fileServices.SaveToWwwrootAsync(fileForm);
            if (error != null) return BadRequest(error);
            return Ok(file);
        }

        [HttpGet("get-Temp-file")]
        public async Task<IActionResult> GetFile([FromQuery] string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return BadRequest("File path is required.");

            var result = await _fileServices.GetFileAsync(filePath);

            if (result.error != null)
                return NotFound(result.error);

            if (result.fileStream == null)
                return NotFound("File not found.");

            return File(result.fileStream, result.contentType ?? "application/octet-stream");

        }

        [HttpDelete("temp-files")]
        public async Task<IActionResult> RemoveAllTempFolderFiles()
        {
            var result = await _fileServices.RemoveAllTempFolderFilesAsync();
            if (result)
                return Ok(new { success = true, message = "All temp files removed successfully." });
            return StatusCode(500, new { success = false, message = "Failed to remove temp files." });
        }

        [HttpPost("decrypt-and-install")]
        public async Task<IActionResult> DecryptAndInstall([FromBody] List<string> fileUrls, [FromQuery] string archiveName = "DecryptedFiles")
        {
            if (fileUrls == null || !fileUrls.Any())
                return BadRequest("File URLs are required.");
            var (archivePath, error) = await _fileServices.CopyFilesToDesktopAsync(fileUrls, archiveName);
            if (error != null)
                return BadRequest(error);
            return Ok(new { archivePath });
        }
    }
}
