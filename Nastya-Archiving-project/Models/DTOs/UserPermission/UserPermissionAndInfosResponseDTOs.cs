using Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Nastya_Archiving_project.Models.DTOs.UserPermission
{
    public class UserPermissionAndInfosResponseDTOs
    {
        public int Id { get; set; }
        public string? username { get; set; } = null!;
        public string? RealName { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string? PhoneNumber { get; set; } = null!;
        public string? Address { get; set; } = null!;
        public string? IsActive { get; set; }

        public List<UsersOptionPermission> usersOptionPermissions { get; set; }
        public List<DepartmentResponseDTOs> UserDepartement { get; set; }
    }
}
    