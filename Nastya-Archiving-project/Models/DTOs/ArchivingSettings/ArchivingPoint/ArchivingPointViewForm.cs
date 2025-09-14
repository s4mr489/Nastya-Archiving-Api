namespace Nastya_Archiving_project.Models.DTOs.ArchivingSettings.ArchivingPoint
{
    public class ArchivingPointViewForm
    {
        public string? pointName { get; set; }
        public int departmentId { get; set; }
        public int branchId { get; set; }
        public int accountUnitId { get; set; }
        public string? startWith { get; set; }
    }
}
