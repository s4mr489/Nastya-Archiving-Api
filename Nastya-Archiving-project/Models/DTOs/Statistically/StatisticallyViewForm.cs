using Microsoft.VisualBasic;

namespace Nastya_Archiving_project.Models.DTOs.Statistically
{
    public class StatisticallyViewForm
    {
        public int? year { get; set; }
        public int? month { get; set; }
        public DateTime? fromEditingDate { get; set; }
        public DateTime? toEditingDate { get; set; }
        public DateTime? fromDocDate { get; set; }
        public DateTime? toDocDate { get; set; }
        public List<int?>? departmentId { get; set; }
        public List<int?>? docSourceId { get; set; } // For source organizations
        public List<int?>? docTargetId { get; set; } // For target organizations
        public List<int?>? docTypeId { get; set; } // For document types
        public List<int?>? supDocTypeId { get; set; } // For supplementary document types
        public OutputType? outputType { get; set; } = OutputType.All; // For output selection
        public List<string>? editorIds { get; set; } // For specific editors

        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 20;
    }

    public enum OutputType
    {
        All = 0,
        DepartmentsOnly = 1,
        EmployeesOnly = 2
    }
}
