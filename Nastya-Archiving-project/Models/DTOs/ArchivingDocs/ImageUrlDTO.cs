namespace Nastya_Archiving_project.Models.DTOs.ArchivingDocs
{
    public class ImageUrlDTO
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = "";
        public string? DocNo { get; set; }
        public string? DocTitle { get; set; }
        public string? ReferenceNo { get; set; }
    }
}
