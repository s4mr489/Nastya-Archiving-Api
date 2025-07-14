using System.util;

namespace Nastya_Archiving_project.Models.DTOs.Auth
{
    public class ChangePasswordViewFrom
    {
        public string? CurrnetPassword { get; set; }
        public string? newPassword { get; set; }
    }
}
