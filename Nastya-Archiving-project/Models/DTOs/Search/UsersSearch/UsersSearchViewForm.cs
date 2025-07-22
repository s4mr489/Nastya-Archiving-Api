using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.ArchivingPoint;

namespace Nastya_Archiving_project.Models.DTOs.Search.UsersSearch
{
    public class UsersSearchViewForm
    {
        public int? accountUnitId { get; set; }
        public int? branchId { get; set; }
        public int? departmentId { get; set; }

        public int? pageSize { get; set; }
        public int? pageNumber { get; set; }
    }

    public class UsersViewForm
    {
        public int? Id { get; set; }
    }
}
