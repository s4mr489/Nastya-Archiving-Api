using iText.StyledXmlParser.Node;

namespace Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch
{
    public class SearchDeletedDocsViewForm
    {
        public int? accountUnitId { get; set; }
        public int? branchId { get; set; }
        public int? DepartId { get; set; }

        public int? pageList { get; set; } = 1;
        public int? pageSize { get; set; } = 20;
    }
}
