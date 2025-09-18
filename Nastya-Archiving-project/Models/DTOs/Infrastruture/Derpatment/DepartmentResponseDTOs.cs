namespace Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment
{
    public class DepartmentResponseDTOs
    {
        public int Id { get; set; }
        public string? DepartmentName { get; set; }
        public int? BranchId { get; set; }
        public int? AccountUnitId { get; set; }
        public bool? hasArchivingPoint { get; set; }

    }
}
