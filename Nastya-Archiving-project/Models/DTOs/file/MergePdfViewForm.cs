namespace Nastya_Archiving_project.Models.DTOs.file
{
    public class MergePdfViewForm
    {
        public string OriginalFilePath { get; set; } // Absolute path to the original file on disk
        public IFormFile File { get; set; }
    }
}
