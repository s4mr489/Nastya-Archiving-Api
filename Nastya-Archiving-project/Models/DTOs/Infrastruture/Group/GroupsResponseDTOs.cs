namespace Nastya_Archiving_project.Models.DTOs.Infrastruture.Group
{
    public class GroupsResponseDTOs
    {
        public int groupId { get; set; }
        public string? groupDscrp { get; set; }
        public string? Editor { get; set; }
        public DateOnly? EditDate { get; set; }
        public int? AccountUnitId { get; set; }
    }
}
