using Nastya_Archiving_project.Helper.Enums;

namespace Nastya_Archiving_project.Models.DTOs.Search.TreeSearch
{
    public class TreeSearchViewForm
    {
        public DateTime? from { get; set; }
        public DateTime? to { get; set; }
        public string? editor { get; set; }
        public int? departId { get; set; }
        public int? docsType { get; set; }
        public int? supDocsType { get; set; }
        public int? source { get; set; }
        public int? target { get; set; }
        public string? noitce { get; set; }
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 20;
    }
}
