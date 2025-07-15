using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.file;

namespace Nastya_Archiving_project.Services.files
{
    public interface IFilesServices
    {
        Task<(string? file, long fileSize, string? error)> upload(FileViewForm fileForm);
        Task<(List<(string filePath, string fileType, string? notice)> files, string? error)> uploadWithType(MultiFileFormViewForm filesForm);
        Task<(IActionResult, string? error)> DownloadPdf(MultiFileFormViewForm filesForm);
        Task<string> GetHtml(MultiFileFormViewForm filesForm);

        Task<(string? file, string? error)> SaveToWwwrootAsync(FileViewForm fileForm);

        Task<(Stream? fileStream, string? fileName, string? contentType, string? error)> GetDecryptedFileByPathAsync(string filePath);
        bool RemoveTempUserFile(string fileName);

        Task<List<string>> GetTempFolderFilesAsync();
        Task<(Stream? fileStream, string? contentType, string? error)> GetFileAsync(string relativePath);

        Task<(byte[]? MergedFile, string? Error)> MergeTwoPdfFilesAsync(MergePdfViewForm requestDTO);
        long GetFileSize(IFormFile file);
    }
}
