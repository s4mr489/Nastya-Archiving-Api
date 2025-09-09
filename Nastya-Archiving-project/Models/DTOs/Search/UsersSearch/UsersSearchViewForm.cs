using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.ArchivingPoint;

namespace Nastya_Archiving_project.Models.DTOs.Search.UsersSearch
{
    public class UsersSearchViewForm
    {
        public int? accountUnitId { get; set; }
        public int? branchId { get; set; }
        public int? departmentId { get; set; }
        public string? userRealName { get; set; }

        public int? pageSize { get; set; } = 15;
        public int? pageNumber { get; set; } = 1;
    }

    public class UsersViewForm
    {
        public int? Id { get; set; }
    }
}
