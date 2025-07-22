using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Models.DTOs.UserPermission;

namespace Nastya_Archiving_project.Services.usersPermission
{
    public interface IUserPermissionsServices
    {
        Task<List<UsersSearchResponseDTOs>> GetUsersAsync(UsersSearchViewForm search);
        Task<BaseResponseDTOs> GetUserPermissionsAsync(UsersViewForm users);
        Task<BaseResponseDTOs> CreateUserPermissionsAsync(CreateUserPermissionsRequestDTO request);
        Task<BaseResponseDTOs> DeleteUserPermissionsAsync(CreateUserPermissionsRequestDTO request);
    }
}
