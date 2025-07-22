namespace Nastya_Archiving_project.Models.DTOs.UserPermission
{
    public class CreateUserPermissionsRequestDTO
    {
        public int UserId { get; set; }
        public List<int>? ArchivingPointIds { get; set; }
        public int? FileTypeId { get; set; }
        public int? addParameters { get; set; }

        public int? allowDelete { get; set; }

        public int? allowAddToOther { get; set; }

        public int? allowViewTheOther { get; set; }

        public int? allowSendMail { get; set; }

        public int? allowDownload { get; set; }
    }
}
