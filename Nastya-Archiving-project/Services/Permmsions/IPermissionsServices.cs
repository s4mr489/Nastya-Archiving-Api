using Nastya_Archiving_project.Models.DTOs;

namespace Nastya_Archiving_project.Services.Permmsions
{
    public interface IPermissionsServices
    {
        Task<string> AddGroupPermissions(int groupId, List<int> permissionsIds);
        Task<string> DeletedGroupPermissions(int groupId, List<int> permissionsIds);
        Task<(List<string>? permmsion, string? error)> GetPermissionsByGroupId(int groupId);

        Task<BaseResponseDTOs> GetAllPermissions();
    }
}
