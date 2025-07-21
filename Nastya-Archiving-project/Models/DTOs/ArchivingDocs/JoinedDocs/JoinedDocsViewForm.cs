using System.util;

namespace Nastya_Archiving_project.Models.DTOs.ArchivingDocs.JoinedDocs
{
    public class JoinedDocsViewForm
    {
        public string? parentReferenceId { get; set; }
        public string? childReferenceId { get; set; }
        public int? BreafcaseNo { get; set; }
    }
}
