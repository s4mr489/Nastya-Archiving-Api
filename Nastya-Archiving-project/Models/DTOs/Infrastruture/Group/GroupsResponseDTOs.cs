namespace Nastya_Archiving_project.Models.DTOs.Infrastruture.Group
{
    public class GroupsResponseDTOs
    {
        public int groupId { get; set; }
        public string? groupDscrp { get; set; }
        public string? Editor { get; set; }
        public DateOnly? EditDate { get; set; }
        public int? AccountUnitId { get; set; }
        public int? AllowDownload { get; set; } = 0;
        public int? AllowSendMail { get; set; } = 0;
        public int? AllowViewTheOther { get; set; } = 0;
        public int? AllowAddToOther { get; set; } = 0;
        public int? AllowDelete { get; set; } = 0;
        public int? AddParameters { get; set; } = 0;
    }
}
