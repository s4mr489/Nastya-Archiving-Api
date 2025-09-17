using Microsoft.Identity.Client;

namespace Nastya_Archiving_project.Models.DTOs.ArchivingSettings.SupDocsType
{
    public class SupDocsTypeViewform
    {
        public string? supDocuName { get; set; }
        public int DocTypeId { get; set; }
        public int accountUnitId { get; set; }
        public int branchId { get; set; }
        public int departId { get; set; }
    }
}
