using Nastya_Archiving_project.Models.DTOs.Auth;

namespace Nastya_Archiving_project.Services.auth
{
    public interface IAuthServices
    {
        Task<string> Login(LoginFormDTO form, bool IsAdmin);
    }
}
