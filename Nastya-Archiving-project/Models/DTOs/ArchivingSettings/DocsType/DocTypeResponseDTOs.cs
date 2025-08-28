namespace Nastya_Archiving_project.Models.DTOs.ArchivingSettings.DocsType
{
    public class DocTypeResponseDTOs
    {
        public int Id { get; set; }
        public string docuName { get; set; }
        public int departmentId { get; set; }
        public int branchId { get; set; }
        public int AccountUnitId { get; set; }
        public string? isCode { get; set; }
        public string? departmentName { get; set; }
    }
}
