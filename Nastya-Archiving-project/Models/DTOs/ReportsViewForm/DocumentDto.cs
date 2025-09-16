namespace Nastya_Archiving_project.Models.DTOs.Reports
{
    public class DocumentDto
    {
        public DateTime? DocDate { get; set; }
        public DateTime? EditDate { get; set; }
        public string DocNo { get; set; }
        public string Subject { get; set; }
        public string Editor { get; set; }
        public int DocMonth { get; set; }
        public string DepartmentName { get; set; }
        public int DepartmentId { get; set; }
        public int fileSize { get; set; }
        public int fileName { get; set; }
    }
}
