namespace Nastya_Archiving_project.Models.DTOs.Auth
{
    public class UsersResponseDTOs
    {
        public int Id { get; set; }
        public string? realName { get; set; }
        public string? userName { get; set; }
        public string? accountUnit { get; set; }
        public string? branch { get; set; }
        public string? depart { get; set; }
        public string? jobTitl { get; set; }
        public string? permission { get; set; }
    }
}
