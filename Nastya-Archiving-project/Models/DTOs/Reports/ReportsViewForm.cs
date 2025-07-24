using DocumentFormat.OpenXml.Wordprocessing;
using Nastya_Archiving_project.Helper.Enums;

namespace Nastya_Archiving_project.Models.DTOs.Reports
{
    public class ReportsViewForm
    {
        public DateOnly? fromArchivingDate { get; set; }
        public DateOnly? toArchivingDate { get; set; }

        public DateTime? fromEditingDate { get; set; }
        public DateTime? toEditingDate { get; set; }

        public int? docTypeId { get; set; }
        public int? sourceId { get; set; }
        public int? toId { get; set; }

        public List<int?>? departmentId { get; set; }
        public EReportType? reportType { get; set; }
        public EResultType? resultType { get; set; }

        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 20;
    }
}
