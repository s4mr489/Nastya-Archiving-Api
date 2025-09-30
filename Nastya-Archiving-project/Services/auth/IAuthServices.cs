using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Auth;
using System.Runtime.CompilerServices;

namespace Nastya_Archiving_project.Services.auth
{
    public interface IAuthServices
    {

        Task<string> Login(LoginFormDTO form, bool IsAdmin);
        Task<(RegisterResponseDTOs user,string? error)> Register(RegisterViewForm form, bool IsAdmin = false);
        Task<string> ChangeUserPassword(ChangePasswordViewFrom pass);
        Task<string> RemoveUser(int Id);
        Task<(PagedList<UsersResponseDTOs>? users, string? error)> GetAllUsers(string realName,int pageNumber = 1, int pageSize = 10);
        Task<(UsersResponseDTOs? user, string? error)> SearchUsers(string? realName, int departId, string? userName);
        Task<(RegisterResponseDTOs? user, string? error)> EditUser(int id, RegisterViewForm form, bool IsAdmin);
        Task<BaseResponseDTOs> GetDepartForUsers(int userId);
        Task<BaseResponseDTOs> FirstUsers(LoginFormDTO req);
        Task<BaseResponseDTOs> ActiveOrDeActivUser(int Id, bool status);
    }
}
