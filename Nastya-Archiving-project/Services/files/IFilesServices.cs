using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.file;
using System.Runtime.CompilerServices;

namespace Nastya_Archiving_project.Services.files
{
    public interface IFilesServices
    {

        /// <summary>
        /// this Implmention for File in all the project like upload to database 
        /// </summary>
        /// <param name="fileForm"></param>
        /// <returns></returns>
         
        ///this used for encrypt and comperss then upload to the path database and file to sever 
        Task<(string? file, long fileSize, string? error)> upload(FileViewForm fileForm);
        /// this for upload to the tmep file inside the project with user RealName inside the wwwroot
        Task<(List<(string filePath, string fileType, string? notice)> files, string? error)> uploadWithType(MultiFileFormViewForm filesForm);
        //that use to download the file we don't use it on the project until now 
        Task<(IActionResult, string? error)> DownloadPdf(MultiFileFormViewForm filesForm);
        //don't used until now
        Task<string> GetHtml(MultiFileFormViewForm filesForm);
        //that use for upload the system icons to wwwroot 
        Task<(string? file, string? error)> SaveToWwwrootAsync(FileViewForm fileForm);
        //that use to decompress and decrypte the file and show it when we want 
        Task<(Stream? fileStream, string? contentType, string? error)> GetDecryptedFileStreamAsync(string filePath);
        //that use to remove file from the temp folder for the user after uploading it 
        bool RemoveTempUserFile(string fileName);
        // that use to get all the tempFolder for user when he open the archivign page
        Task<List<(string FileName, long FileSize)>> GetTempFolderFilesAsync();
        //taht implement to read the file from the temp folder for the user 
        Task<(Stream? fileStream, string? contentType, string? error)> GetFileAsync(string relativePath);
        // that use when we need to update the docs file and insert new pdf to the old pdf
        Task<(byte[]? MergedFile, string? Error)> MergeTwoPdfFilesAsync(MergePdfViewForm requestDTO);
        //that use to get the file size
        long GetFileSize(IFormFile file);
        //this is used to merge docx files into one file
        Task<(byte[]? MergedFile, string? FileName, string? Error)> MergeDocxFilesAsync(List<IFormFile> files);
        //this is used to remove all the temp folder files for the user
        Task<bool> RemoveAllTempFolderFilesAsync();
        // this method is used to download the file from the serve and decrypt it then save it to the desktop
        Task<(string? archivePath, string? error)> DecryptAndInstallToDesktopAsync(List<string> fileUrls, string archiveName = "DecryptedFiles");
    }
}
