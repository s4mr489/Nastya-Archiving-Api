using iText.Pdfua.Checkers.Utils.Ua1;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;

namespace Nastya_Archiving_project.Models.DTOs.ArchivingSettings.DocsType
{
    public class DocTypeViewform
    {
        public string docuName { get; set; }
        public int departmentId { get; set; }
        public int branchId { get; set; }
        public int AccountUnitId { get; set; }
        public string? isCode { get; set; }
    }
}
