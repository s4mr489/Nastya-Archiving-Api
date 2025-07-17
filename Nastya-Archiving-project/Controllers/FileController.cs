using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Services.files;

namespace Nastya_Archiving_project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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

        [HttpGet("temp-files")]
        public async Task<IActionResult> GetTempFiles()
        {
            var files = await _fileServices.GetTempFolderFilesAsync();
            return Ok(files);
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
            var (fileStream, fileName, contentType, error) = await _fileServices.GetDecryptedFileByPathAsync(filePath);
            if (error != null || fileStream == null) return NotFound(error);
            return File(fileStream, contentType ?? "application/octet-stream", fileName);
        }

        [HttpPost("save-wwwroot")]
        public async Task<IActionResult> SaveToWwwroot([FromForm] FileViewForm fileForm)
        {
            var (file, error) = await _fileServices.SaveToWwwrootAsync(fileForm);
            if (error != null) return BadRequest(error);
            return Ok(file);
        }
    }
}
