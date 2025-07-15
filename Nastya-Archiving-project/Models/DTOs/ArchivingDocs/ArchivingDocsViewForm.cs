using System.ComponentModel.DataAnnotations;

namespace Nastya_Archiving_project.Models.DTOs.ArchivingDocs
{
    public class ArchivingDocsViewForm
    {
        public string? RefrenceNo { get; set; }

        public string? DocNo { get; set; }

        public DateOnly? DocDate { get; set; }

        public int? DocSource { get; set; }

        public int? DocTarget { get; set; }

        public string? Subject { get; set; }

        public string? WordsTosearch { get; set; }

        public string? DocTitle { get; set; }

        public string? BoxfileNo { get; set; }

        public int DocType { get; set; }

        public int DepartId { get; set; }

        public string? Editor { get; set; }

        public DateTime? EditDate { get; set; }

        public int AccountUnitId { get; set; }

        public int? Theyear { get; set; }

        public decimal? DocSize { get; set; }

        public int BranchId { get; set; }

        public string? Notes { get; set; }

        public int FileType { get; set; }

        public int? TheMonth { get; set; }

        public int? SubDocType { get; set; }

        public string? Ipaddress { get; set; }

        public int? HaseBakuped { get; set; }

        public string? ReferenceTo { get; set; }
    }
}
