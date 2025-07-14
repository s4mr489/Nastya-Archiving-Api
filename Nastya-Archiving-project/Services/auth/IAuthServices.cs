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
        Task<(UsersResponseDTOs? user,string? error)> GetAllUsers();
        Task<(UsersResponseDTOs? user, string? error)> SearchUsers(string? realName ,string? userName);
        Task<(RegisterResponseDTOs? user, string? error)> EditUser(int id, RegisterViewForm form, bool IsAdmin);
    }
}
