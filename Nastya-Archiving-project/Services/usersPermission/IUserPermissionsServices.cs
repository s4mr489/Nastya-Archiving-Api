using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Models.DTOs.UserPermission;

namespace Nastya_Archiving_project.Services.usersPermission
{
    public interface IUserPermissionsServices
    {
        Task<List<UsersSearchResponseDTOs>> GetUsersAsync(UsersSearchViewForm search);
        Task<BaseResponseDTOs> GetUserPermissionsAsync(UsersViewForm users);
        /// <summary>
        /// this method is used to create the permissions of a user
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BaseResponseDTOs> CreateUserPermissionsAsync(CreateUserPermissionsRequestDTO request);

        /// <summary>
        /// this method is used to delete the permissions of a user
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BaseResponseDTOs> DeleteUserPermissionsAsync(CreateUserPermissionsRequestDTO request);
    }
}
