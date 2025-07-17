using iText.StyledXmlParser.Node;
using Microsoft.Identity.Client;
using Nastya_Archiving_project.Helper.Enums;
using Org.BouncyCastle.Pqc.Crypto.Frodo;

namespace Nastya_Archiving_project.Models.DTOs.Search.QuikSearch
{
    public class QuikeSearchViewForm
    {
        public string? systemId { get; set; }
        public string? docsNumber { get; set; }
        public bool? docsDate { get; set; }
        public bool? editDate { get; set; }
        public DateTime? from { get; set; }
        public DateTime? to { get; set; }
        public string? subject { get; set; }
        public string? docsType { get; set; }
        public string? supDocsType { get; set; }
        public string? source { get; set; }
        public string? ReferenceTo { get; set; }
        public string? wordToSearch { get; set; } 
        public string? boxFile { get; set; }
        public EFileType? fileType { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
    }
}
