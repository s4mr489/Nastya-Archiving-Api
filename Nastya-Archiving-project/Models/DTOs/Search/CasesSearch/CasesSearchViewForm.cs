namespace Nastya_Archiving_project.Models.DTOs.Search.CasesSearch
{
    public class CasesSearchViewForm
    {
        public string? CaseNumber { get; set; }
        public DateTime? from { get; set; }
        public DateTime? to { get; set; }

        public int pageSize { get; set; } = 15;
        public int pageNumber { get; set; } = 1;

    }
}
