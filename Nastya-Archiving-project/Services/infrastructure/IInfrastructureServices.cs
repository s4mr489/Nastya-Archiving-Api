using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.AccountUnit;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Branch;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Group;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.GroupForm;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.JobTitle;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Organization;

namespace Nastya_Archiving_project.Services.infrastructure
{
    public interface IInfrastructureServices
    {
        /// Account Unit Implementation
        Task<(AccountUnitResponseDTOs? accountUnit ,string? error)> PostAccountUint(AccountUnitViewForm req);
        Task<(AccountUnitResponseDTOs? accountUnit , string? error)> EditAccountUint(AccountUnitViewForm req , int Id);
        Task<(List<AccountUnitResponseDTOs>? accountUnits , string? error)> GetAllAccountUint();
        Task<(AccountUnitResponseDTOs? accountUnits, string? error)> GetAccountUintById(int Id);
        Task<string> DeleteAccountUint(int accountId);

        //Users Group Implementation
        Task<(GroupsResponseDTOs? group, string? error)> PostGroup(GroupViewForm req);
        Task<(GroupsResponseDTOs? group, string? error)> EditGroup(GroupViewForm req, int Id);
        Task<(List<GroupsResponseDTOs>? group, string? error)> GetAllGroups();
        Task<(GroupsResponseDTOs? group, string? error)> GetGrouptById(int Id);
        Task<string> DeleteGroup(int Id);

        //Branch Implementation
        Task<(BranchResponseDTOs? Branch, string? error)> PostBranch(BranchViewForm req);
        Task<(BranchResponseDTOs? Branch, string? error)> EditBranch(BranchViewForm req, int Id);
        Task<(List<BranchResponseDTOs>? Branch, string? error)> GetAllBranches();
        Task<(BranchResponseDTOs? Branch, string? error)> GetBranchById(int Id);
        Task<string> DeleteBranch(int Id);

        //Department Implementation

        Task<(DepartmentResponseDTOs? Department, string? error)> PostDepartment(DepartmentViewForm req);
        Task<(DepartmentResponseDTOs? Department, string? error)> EditDeparment(DepartmentViewForm req, int Id);
        Task<(List<DepartmentResponseDTOs>? Department, string? error)> GetAllDepartment();
        Task<(DepartmentResponseDTOs? Department, string? error)> GetDepartmentById(int Id);
        Task<string> DeleteDepartment(int Id);

       //POrganization Implementation
        Task<(OrgniztionResponseDTOs? POrganization, string? error)> PostPOrganization(OrgniztionViewForm req);
        Task<(OrgniztionResponseDTOs? POrganization, string? error)> EditPOrganization(OrgniztionViewForm req, int Id);
        Task<(List<OrgniztionResponseDTOs>? POrganization, string? error)> GetAllPOrganizations();
        Task<(OrgniztionResponseDTOs? POrganization, string? error)> GetPOrganizationById(int Id);
        Task<string> DeletePOrganization(int Id);

        // Job Title Implementation
        Task<(JobTitleResponseDTOs? Job, string? error)> PostJobTitle(JobTitleViewForm req);
        Task<(JobTitleResponseDTOs? Job, string? error)> EditJobTitle(JobTitleViewForm req, int Id);
        Task<(List<JobTitleResponseDTOs>? Job, string? error)> GetAllJobTitle();
        Task<(JobTitleResponseDTOs? Job, string? error)> GetJobTitleById(int Id);
        Task<string> DeleteJobTitle(int Id);
    }
}
