using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Nastya_Archiving_project.Models.DTOs.Infrastruture.GroupForm
{
    public class GroupViewForm
    {
        public string? groupDscrp { get; set; } 
        public int ? AccountUnitId { get; set; }
        public int? AllowDownload { get; set; } = 0;
        public int? AllowSendMail { get; set; } = 0;
        public int? AllowViewTheOther { get; set; } = 0;
        public int? AllowAddToOther { get; set; } = 0;
        public int? AllowDelete { get; set; } = 0;
        public int? AddParameters { get; set; } = 0;
    }
}
