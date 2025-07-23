using Org.BouncyCastle.Crypto.Digests;

namespace Nastya_Archiving_project.Helper.Enums
{
    public enum EReportType
    {
        GeneralReport = 0,
        ByDepartment = 1,
        ByDepartmentAndUsers = 2,
        ByDepartmentAndMount = 3,
        ByUsersAndMount = 4,
        BySendingOrgnization = 5,
        ByReceivingOrgnization = 6,
        ByReferenceTo = 7,
    }
}
