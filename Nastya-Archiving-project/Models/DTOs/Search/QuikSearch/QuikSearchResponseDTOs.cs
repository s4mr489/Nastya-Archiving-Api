using DocumentFormat.OpenXml.Spreadsheet;

namespace Nastya_Archiving_project.Models.DTOs.Search.QuikSearch
{
    public class QuikSearchResponseDTOs
    {
        public string? systemId { get; set; }
        public int Id { get; set; }
        public string? file { get; set; }
        public string? docsNumber { get; set; }
        public DateOnly? docsDate { get; set; }
        public DateTime? editDate { get; set; }
        public string? docsTitle { get; set; }
        public int? departId { get; set; }
        public string? subject { get; set; }
        public string? source { get; set; }
        public string? ReferenceTo { get; set; }
        public string? fileType { get; set; }
        public string? docType { get; set; }
        public bool? HasJoinedDocs { get; set; }
    }
}