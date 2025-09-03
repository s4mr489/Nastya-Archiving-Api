namespace Nastya_Archiving_project.Models.DTOs.Search.UsersSearch
{
    public class UsersSearchResponseDTOs
    {
        public int? userId { get; set; }
        public int? fileType { get; set; }
        public string? archiveDscrp { get; set; }
        public int? Activation { get; set; }
        public string? realName { get; set; }
        public UsersOptionPermission? usersOptionPermission { get; set; }
        public List<ArchivingPermissionResponseDTOs>? archivingPoint { get; set; }
    }
}
