using Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment;
using Org.BouncyCastle.Bcpg.OpenPgp;
    
namespace Nastya_Archiving_project.Models.DTOs.UserPermission
{
    public class UserPermissionAndInfosResponseDTOs
    {
        public int Id { get; set; }
        public string username { get; set; }
        public string RealName { get; set; }
        public int? accountUnit { get; set; }
        public string accountUnitDscrp { get; set; }
        public int? branch { get; set; }
        public string branchDscrp { get; set; }
        public int? depart { get; set; }
        public string departDscrp { get; set; }
        public int? group { get; set; }
        public string groupDscrp { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string IsActive { get; set; }
        public List<UsersOptionPermission> usersOptionPermissions { get; set; }
        public List<DepartmentResponseDTOs> UserDepartement { get; set; }
    }
}
    