using Org.BouncyCastle.Tls;

namespace Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch
{
    public class DeletedDocsResponseDTOs
    {
        public int? Id { get; set; }
        public string? systemId { get; set; }
        public string? docNO { get; set; }
        public DateTime? docDate { get; set; }
        public string? source { get; set; }
        public string? to { get; set; }
        public string? subject { get; set; }
        public int? docuType { get; set; }
        public string? noitce { get; set; }
        public string? editor { get; set; }
        public string? editDocs { get; set; }
    }
}
