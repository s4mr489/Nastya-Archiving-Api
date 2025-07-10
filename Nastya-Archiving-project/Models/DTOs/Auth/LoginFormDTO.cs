using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Nastya_Archiving_project.Models.DTOs.Auth
{
    public class LoginFormDTO
    {
        public string userName { get; set; }
        public string password { get; set; } 
    }
}
