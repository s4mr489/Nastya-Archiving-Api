using DocumentFormat.OpenXml.Wordprocessing;
using Nastya_Archiving_project.Helper.Enums;

namespace Nastya_Archiving_project.Models.DTOs.Reports
{
    public class ReportsViewForm
    {
        /// <summary>
        /// Start date for archiving date filter
        /// </summary>
        public DateOnly? fromArchivingDate { get; set; }
        
        /// <summary>
        /// End date for archiving date filter
        /// </summary>
        public DateOnly? toArchivingDate { get; set; }

        /// <summary>
        /// Start date for editing date filter
        /// </summary>
        public DateTime? fromEditingDate { get; set; }
        
        /// <summary>
        /// End date for editing date filter
        /// </summary>
        public DateTime? toEditingDate { get; set; }

        /// <summary>
        /// Filter by document type ID
        /// </summary>
        public int? docTypeId { get; set; }
        
        /// <summary>
        /// Filter by source organization ID
        /// </summary>
        public int? sourceId { get; set; }
        
        /// <summary>
        /// Filter by target organization ID
        /// </summary>
        public int? toId { get; set; }

        /// <summary>
        /// Filter by department IDs (multiple departments can be selected)
        /// </summary>
        public List<int?>? departmentId { get; set; }
        
        /// <summary>
        /// Report type (determines the type of report to generate)
        /// </summary>
        public EReportType? reportType { get; set; }
        
        /// <summary>
        /// Output format for the report (pdf, excel, json, etc.)
        /// </summary>
        public string? outputFormat { get; set; } = "pdf"; // Default to PDF

        /// <summary>
        /// Result type (statistical or detailed)
        /// </summary>
        public EResultType? resultType { get; set; }

        /// <summary>
        /// Custom title for the report
        /// </summary>
        public string? reportTitle { get; set; } = "Report";

        public int? supDocType { get; set; }
        public string? notice { get; set; }
        public string? docNo { get; set; }
        public string? subject { get; set; }
        public string? boxFileNo { get; set; }

        /// <summary>
        /// Page number for department-level pagination (which department to show)
        /// </summary>
        public int pageNumber { get; set; } = 1;
        
        /// <summary>
        /// Number of items per page
        /// </summary>
        public int pageSize { get; set; } = 20;
        
        /// <summary>
        /// Page number for document-level pagination within a department 
        /// (which set of documents to show within the current department)
        /// </summary>
        public int docPage { get; set; } = 1;
    }
}
