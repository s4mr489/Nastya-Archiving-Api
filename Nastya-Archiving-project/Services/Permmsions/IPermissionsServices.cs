using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;

namespace Nastya_Archiving_project.Services.Permmsions
{
    public interface IPermissionsServices
    {
        Task<string> AddGroupPermissions(int groupId, List<int> permissionsIds);
        Task<string> DeletedGroupPermissions(int groupId, List<int> permissionsIds);
        Task<(List<string>? permmsion, string? error)> GetPermissionsByGroupId(int groupId);

        Task<BaseResponseDTOs> GetAllPermissions();
        Task<BaseResponseDTOs> CopyUserPermissionForGroup(int Id);
        Task<BaseResponseDTOs> CopyUserPermissionToMultipleUsers(int sourceUserId, List<int> targetUserIds);
        /// <summary>
        /// Copies permissions from a group to all users belonging to that group
        /// </summary>
        /// <param name="groupId">The ID of the group whose permissions will be copied to its users</param>
        /// <returns>A response indicating success or failure of the operation</returns>
        Task<BaseResponseDTOs> CopyGroupPermissionsToUsersAsync(int groupId);
       
    }
}
