using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.AccountUnit;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Branch;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Group;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.GroupForm;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.JobTitle;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Organization;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.SystemInfo;

namespace Nastya_Archiving_project.Services.infrastructure
{
    public class InfrastructureServices : BaseServices, IInfrastructureServices
    {
        private readonly AppDbContext _context;
        private readonly ISystemInfoServices _systemInfoServices;
        private readonly IEncryptionServices _encryptionServices;
        public InfrastructureServices(AppDbContext context, ISystemInfoServices systemInfoServices, IEncryptionServices encryptionServices) : base(null, context)
        {
            _context = context;
            _systemInfoServices = systemInfoServices;
            _encryptionServices = encryptionServices;
        }

        public async Task<(AccountUnitResponseDTOs? accountUnit, string? error)> PostAccountUint(AccountUnitViewForm req)
        {
            // Check if the account unit Dscrp already exists
            var accountUnit = await _context.GpAccountingUnits.FirstOrDefaultAsync(e => e.Dscrp == req.accountUnitName);
            if(accountUnit != null)
                return (null, "400");

            var newAccountUnit = new GpAccountingUnit
            {
                Dscrp = req.accountUnitName,
            };
            _context.GpAccountingUnits.Add(newAccountUnit);
            await _context.SaveChangesAsync();

            var rsponse = new AccountUnitResponseDTOs
            {
                Id = newAccountUnit.Id,
                accountUnitDscrp = newAccountUnit.Dscrp,
            };

            return (rsponse, null);
        }
        public async Task<(AccountUnitResponseDTOs? accountUnits, string? error)> GetAccountUintById(int accountId)
        {
            // Check if the account unit exists by Id
            var accountUnit = await _context.GpAccountingUnits.FirstOrDefaultAsync(e => e.Id == accountId);
            if (accountUnit == null)
                return (null, "404");

            var response = new AccountUnitResponseDTOs
            {
                Id = accountUnit.Id,
                accountUnitDscrp = accountUnit.Dscrp,
            };

            return (response , null);
        }

        public async Task<(List<AccountUnitResponseDTOs>? accountUnits, string? error)> GetAllAccountUint()
        {
            // Retrieve all account units from the database
            var accountUnits = await _context.GpAccountingUnits.ToListAsync();
            if (accountUnits == null || accountUnits.Count == 0)
                return (null, "404");

            var response = accountUnits.Select(e => new AccountUnitResponseDTOs
            {
                Id = e.Id,
                accountUnitDscrp = e.Dscrp,
            }).ToList();

            return (response, null);
        }
        public async Task<(AccountUnitResponseDTOs? accountUnit, string? error)> EditAccountUint(AccountUnitViewForm req , int Id)
        {
            // Find the existing account unit by Id (assuming req.Id exists)
            var accountUnit = await _context.GpAccountingUnits.FirstOrDefaultAsync(e => e.Id == Id);
            if (accountUnit == null)
                return (null, "404");

            // Check for duplicate name (excluding current)
            var duplicate = await _context.GpAccountingUnits
                .FirstOrDefaultAsync(e => e.Dscrp == req.accountUnitName);
            if (duplicate != null)
                return (null, "400");

            // Update properties
            accountUnit.Dscrp = req.accountUnitName;

            _context.GpAccountingUnits.Update(accountUnit);
            await _context.SaveChangesAsync();

            var response = new AccountUnitResponseDTOs
            {
                Id = accountUnit.Id,
                accountUnitDscrp = accountUnit.Dscrp,
            };

            return (response, null);
        }
        public async Task<string> DeleteAccountUint(int accountId)
        {
            // Check if the account unit exists by Id
            var accountUnit = await _context.GpAccountingUnits.FirstOrDefaultAsync(e => e.Id  == accountId);
            if( accountUnit == null)
                return "404";

            _context.GpAccountingUnits.Remove(accountUnit);
            await _context.SaveChangesAsync();

            return "200"; // Success
        }

        public async Task<(GroupsResponseDTOs? group, string? error)> PostGroup(GroupViewForm req)
        {
            var userId =await _systemInfoServices.GetRealName();
            if(userId.RealName == null) 
                return (null , userId.error);

            // Check if the group name already exists
            var checkGroup = await _context.Usersgroups.FirstOrDefaultAsync(e => e.Groupdscrp == req.groupDscrp);
            if( checkGroup != null)
                return (null, "400"); // Group name already exists

            var accountUnit = await _context.GpAccountingUnits.FirstOrDefaultAsync(e => e.Id == req.AccountUnitId);
            if(accountUnit == null)
                return (null, "404"); // Account unit not found

            var newGroup = new Usersgroup
            {
                Groupdscrp = _encryptionServices.EncryptString256Bit(req.groupDscrp),
                Editor = userId.RealName.ToString(), // Assuming Editor is a string representation of the user ID
                AccountUnitId = req.AccountUnitId,
                EditDate = DateOnly.FromDateTime(DateTime.Now),
                AllowAddToOther = req.AllowAddToOther,
                AllowDelete = req.AllowDelete,
                AddParameters = req.AddParameters,
                AllowDownload = req.AllowDownload,
                AllowSendMail = req.AllowSendMail,
                AllowViewTheOther = req.AllowViewTheOther
            };

            _context.Usersgroups.Add(newGroup);
            await _context.SaveChangesAsync();

            var response = new GroupsResponseDTOs
            {
                groupId = newGroup.groupid,
                groupDscrp = _encryptionServices.EncryptString256Bit(newGroup.Groupdscrp),
                Editor = newGroup.Editor,
                EditDate = newGroup.EditDate,
                AccountUnitId = newGroup.AccountUnitId,
                AddParameters = newGroup.AddParameters,
                AllowAddToOther = newGroup.AllowAddToOther,
                AllowDelete = newGroup.AllowDelete,
                AllowDownload = newGroup.AllowDownload,
                AllowSendMail = newGroup.AllowSendMail,
                AllowViewTheOther = newGroup.AllowViewTheOther
            };
            return (response, null);
        }


        //group Logic

        public async Task<(GroupsResponseDTOs? group, string? error)> EditGroup(GroupViewForm req, int Id)
        {
            var userId = await _systemInfoServices.GetUserId();
            if (userId.Id == null)
                return (null, userId.error);

            // Find the existing group by Id
            var group = await _context.Usersgroups.FirstOrDefaultAsync(e => e.groupid == Id);
            if (group == null)
                return (null, "404");

            // Check for duplicate group name (excluding current)
            var duplicate = await _context.Usersgroups
                .FirstOrDefaultAsync(e => e.Groupdscrp == req.groupDscrp);
            if (duplicate != null)
                return (null, "400");

            // Update properties
            group.Groupdscrp = _encryptionServices.EncryptString256Bit(req.groupDscrp);
            group.Editor = userId.Id.ToString();
            group.AccountUnitId = req.AccountUnitId;
            group.EditDate = DateOnly.FromDateTime(DateTime.Now);
            group.AllowAddToOther = req.AllowAddToOther ?? group.AllowAddToOther;
            group.AllowDelete = req.AllowDelete ?? group.AllowDelete;
            group.AddParameters = req.AddParameters ?? group.AddParameters;
            group.AllowDownload = req.AllowDownload ?? group.AllowDownload;
            group.AllowSendMail = req.AllowSendMail ?? group.AllowSendMail;
            group.AllowViewTheOther = req.AllowViewTheOther ?? group.AllowViewTheOther;


            _context.Usersgroups.Update(group);
            await _context.SaveChangesAsync();

            var response = new GroupsResponseDTOs
            {
                groupId = group.groupid,
                groupDscrp = _encryptionServices.DecryptString256Bit(group.Groupdscrp),
                Editor = group.Editor,
                EditDate = group.EditDate,
                AccountUnitId = group.AccountUnitId,
                AllowAddToOther = group.AllowAddToOther,
                AllowDelete = group.AllowDelete,
                AddParameters = group.AddParameters,
                AllowDownload = group.AllowDownload,
                AllowSendMail = group.AllowSendMail,
                AllowViewTheOther = group.AllowViewTheOther

            };

            return (response, null);
        }

        public async Task<(List<GroupsResponseDTOs>? group, string? error)> GetAllGroups()
        {
            var groups = await _context.Usersgroups.ToListAsync();
            if (groups == null || groups.Count == 0)
                return (null, "404"); // No groups found

            return (groups.Select(e => new GroupsResponseDTOs
            {
                groupId = e.groupid,
                groupDscrp = _encryptionServices.DecryptString256Bit(e.Groupdscrp),
                Editor = e.Editor,
                EditDate = e.EditDate,
                AccountUnitId = e.AccountUnitId,
                AddParameters = e.AddParameters,
                AllowAddToOther = e.AllowAddToOther,
                AllowDelete = e.AllowDelete,
                AllowDownload = e.AllowDownload,
                AllowSendMail = e.AllowSendMail,
                AllowViewTheOther = e.AllowViewTheOther

            }).ToList(), null);

        }

        public async Task<(GroupsResponseDTOs? group, string? error)> GetGrouptById(int groupId)
        {
            var group = await _context.Usersgroups.FirstOrDefaultAsync(g => g.groupid == groupId);
            if(group == null)
                return (null, "404"); // Group not found

            var response = new GroupsResponseDTOs
            {
                groupId = group.groupid,
                groupDscrp = _encryptionServices.DecryptString256Bit(group.Groupdscrp),
                Editor = group.Editor,
                EditDate = group.EditDate,
                AccountUnitId = group.AccountUnitId,
                AddParameters = group.AddParameters,
                AllowAddToOther = group.AllowAddToOther,
                AllowDelete = group.AllowDelete,
                AllowDownload = group.AllowDownload,
                AllowSendMail = group.AllowSendMail,
                AllowViewTheOther = group.AllowViewTheOther

            };
            return (response, null);
        }

        public async Task<string> DeleteGroup(int groupId)
        {
             var group = await _context.Usersgroups.FirstOrDefaultAsync(g => g.groupid == groupId);
            if (group == null)
                return "404"; // Group not found

            _context.Usersgroups.Remove(group);
            await _context.SaveChangesAsync();
            return("Removed Successfully"); // Success
        }

        //branch Logic

        public async Task<(BranchResponseDTOs? Branch, string? error)> PostBranch(BranchViewForm req)
        {
            var branch = await _context.GpBranches.FirstOrDefaultAsync(b => b.Dscrp == req.branchName);
            if(branch != null)
                return (null, "400"); // Branch name already exists

           await  GetAccountUintById(req.accountUnitId);
            if(GetAccountUintById == null)
                return (null, "404"); // Account unit not found

            var newBranch = new GpBranch
            {
                Dscrp = req.branchName,
                AccountUnitId = req.accountUnitId // Assuming accountUnitId is nullable
            };

            _context.GpBranches.Add(newBranch);
            await _context.SaveChangesAsync();

            var response = new BranchResponseDTOs
            {
                Id = newBranch.Id,
                branchName = newBranch.Dscrp,
                accountUnitId = newBranch.AccountUnitId
            };

            return (response, null); //success insert Branch    
        }

        public async Task<(BranchResponseDTOs? Branch, string? error)> EditBranch(BranchViewForm req, int Id)
        {
            var branch = await _context.GpBranches.FirstOrDefaultAsync(b => b.Id == Id);
            if(branch == null) 
                return (null, "404"); // Branch not found

            await GetAccountUintById(req.accountUnitId);
            if (GetAccountUintById == null)
                return (null, "404"); // Account unit not found

            branch.Dscrp = req.branchName;
            branch.AccountUnitId = req.accountUnitId; // Assuming accountUnitId is nullable

            _context.GpBranches.Update(branch);
            await _context.SaveChangesAsync();

            var response = new BranchResponseDTOs
            {
                Id = branch.Id,
                branchName = branch.Dscrp,
                accountUnitId = branch.AccountUnitId
            };
            return (response , null); //success update Branch
        }

        public async Task<(List<BranchResponseDTOs>? Branch, string? error)> GetAllBranches()
        {
            var branches =await _context.GpBranches.ToListAsync();
            if(branches == null || branches.Count == 0)
                return ((null, "404")); // No branches found

            return (branches.Select(b => new BranchResponseDTOs
            {
                Id = b.Id,
                branchName = b.Dscrp,
                accountUnitId = b.AccountUnitId
            }).ToList(), null); // Return list of branches
        }

        public async Task<(BranchResponseDTOs? Branch, string? error)> GetBranchById(int Id)
        {
          var branch = await _context.GpBranches.FirstOrDefaultAsync(b => b.Id == Id);
            if(branch == null)
                return (null, "404"); // Branch not found

            var response = new BranchResponseDTOs
            {
                Id = branch.Id,
                branchName = branch.Dscrp,
                accountUnitId = branch.AccountUnitId
            };
            return (response, null); // Return branch details
        }

        public async Task<string> DeleteBranch(int Id)
        {
            var branch = await _context.GpBranches.FirstOrDefaultAsync(b => b.Id == Id);
            if (branch == null)
                return ("404"); // Branch not found

            _context.GpBranches.Remove(branch);
            await _context.SaveChangesAsync();

            return "200"; // Success
        }

        // Department Logic

        public async Task<(DepartmentResponseDTOs? Department, string? error)> PostDepartment(DepartmentViewForm req)
        {
            var department =await _context.GpDepartments.FirstOrDefaultAsync(d => d.Dscrp == req.DepartmentName);
            if (department != null)
                return (null, "400"); // Department name already exists

            await GetBranchById(req.BranchId);
            if(GetBranchById == null)
                return (null, "404"); // Branch not found

            await GetAccountUintById(req.AccountUnitId);
            if(GetAccountUintById == null)
                return (null, "404"); // Account unit not found

            var newDepartment = new GpDepartment
            {
                Dscrp = req.DepartmentName,
                BranchId = req.BranchId,
                AccountUnitId = req.AccountUnitId
            };

            _context.GpDepartments.Add(newDepartment);
            await _context.SaveChangesAsync();

            var response = new DepartmentResponseDTOs
            {
                Id = newDepartment.Id,
                DepartmentName = newDepartment.Dscrp,
                BranchId = newDepartment.BranchId, 
                AccountUnitId = newDepartment.AccountUnitId,
            };

            return (response, null); // Success insert Department
        }

        public async Task<(DepartmentResponseDTOs? Department, string? error)> EditDeparment(DepartmentViewForm req, int Id)
        {
            var departmentId = await _context.GpDepartments.FirstOrDefaultAsync(d => d.Id == Id);
            if(departmentId == null)
                return (null, "404"); // Department not found
            var department = await _context.GpDepartments.FirstOrDefaultAsync(d => d.Dscrp == req.DepartmentName);
            if (department != null)
                return (null, "400"); // Department name already exists

            await GetBranchById(req.BranchId);
            if (GetBranchById == null)
                return (null, "404"); // Branch not found

            await GetAccountUintById(req.AccountUnitId);
            if (GetAccountUintById == null)
                return (null, "404"); // Account unit not found

            departmentId.Dscrp = req.DepartmentName;
            departmentId.BranchId = req.BranchId;
            departmentId.AccountUnitId = req.AccountUnitId; 

            _context.Update(departmentId);
            await _context.SaveChangesAsync();

            var response = new DepartmentResponseDTOs
            {
                Id = departmentId.Id,
                DepartmentName = departmentId.Dscrp,
                BranchId = departmentId.BranchId,
                AccountUnitId = departmentId.AccountUnitId,
            };

            return (response, null); // Success update Department

        }

        public async Task<(List<DepartmentResponseDTOs>? Department, string? error)> GetAllDepartment()
        {
            var departments =await _context.GpDepartments.ToListAsync();
            if(departments == null || departments.Count == 0)
                return (null, "404"); // No departments found

            return (departments.Select(d => new DepartmentResponseDTOs
            {
                Id = d.Id,
                DepartmentName = d.Dscrp,
                BranchId = d.BranchId,
                AccountUnitId = d.AccountUnitId
            }).ToList(), null); // Return list of departments
        }

        public async Task<(DepartmentResponseDTOs? Department, string? error)> GetDepartmentById(int Id)
        {
            var department = await _context.GpDepartments.FirstOrDefaultAsync(d => d.Id == Id);
            if (department == null)
                return (null, "404"); // Department not found

            return (new DepartmentResponseDTOs
            {
                Id = department.Id,
                DepartmentName = department.Dscrp,
                BranchId = department.BranchId,
                AccountUnitId = department.AccountUnitId
            }, null); // Return department details
        }

        public async Task<string> DeleteDepartment(int Id)
        {
            var department = await _context.GpDepartments.FirstOrDefaultAsync(d => d.Id == Id);
            if (department == null)
                return ("404"); // Department not found

            _context.GpDepartments.Remove(department);
            await _context.SaveChangesAsync();

            return "200"; // Department Removed Successfully
        }

        // Organization Logic

        public async Task<(OrgniztionResponseDTOs? POrganization, string? error)> PostPOrganization(OrgniztionViewForm req)
        {
            var userId = await _systemInfoServices.GetUserId();
            if (userId.Id == null)
                return (null, "403"); // Unauthorized
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(u => u.UserId.ToString() == userId.Id);
            if (userPermissions.AddParameters == 0)
                return (null, "403"); // Forbidden
            var orgn = await _context.POperations.FirstOrDefaultAsync(o => o.Dscrp== req.Dscrp);
            if(orgn != null)
                return (null, "400"); // Organization already exists

            await GetAccountUintById(req.AccountUnitId);
            if (GetAccountUintById == null)
                return (null, "404"); // Account unit not found

            await GetBranchById(req.BranchId);
            if (GetBranchById == null)
                return (null, "404"); // Branch not found

            await GetDepartmentById(req.DepartId);
            if (GetDepartmentById == null)
                return (null, "404"); // Department not found

           var newOrgn = new POrganization
            {
                Dscrp = req.Dscrp,
                DepartId = req.DepartId,
                AccountUnitId = req.AccountUnitId,
                BranchId = req.BranchId
            };

            _context.POrganizations.Add(newOrgn);
            await _context.SaveChangesAsync();

            var response = new OrgniztionResponseDTOs
            {
                Id = newOrgn.Id,
                Dscrp = newOrgn.Dscrp,
                DepartId = newOrgn.DepartId,
                AccountUnitId = newOrgn.AccountUnitId,
                BranchId = newOrgn.BranchId
            };

            return (response, null); // Success insert Organization
        }

        public async Task<(OrgniztionResponseDTOs? POrganization, string? error)> EditPOrganization(OrgniztionViewForm req, int Id)
        {
            var orgn =await _context.POrganizations.FirstOrDefaultAsync(o => o.Id == Id);
            if (orgn == null)
                return (null, "404"); // Organization not found

            await GetAccountUintById(req.AccountUnitId);
            if (GetAccountUintById == null)
                return (null, "404"); // Account unit not found

            await GetBranchById(req.BranchId);
            if (GetBranchById == null)
                return (null, "404"); // Branch not found

            await GetDepartmentById(req.DepartId);
            if (GetDepartmentById == null)
                return (null, "404"); // Department not found

            orgn.Dscrp = req.Dscrp;
            orgn.DepartId = req.DepartId;
            orgn.AccountUnitId = req.AccountUnitId;
            orgn.BranchId = req.BranchId;

           _context.POrganizations.Update(orgn);
            await _context.SaveChangesAsync();
            var response = new OrgniztionResponseDTOs
            {
                Id = orgn.Id,
                Dscrp = orgn.Dscrp,
                DepartId = orgn.DepartId,
                AccountUnitId = orgn.AccountUnitId,
                BranchId = orgn.BranchId
            };
            return (response, null); // Success update Organization

        }

        public async Task<(List<OrgniztionResponseDTOs>? POrganization, string? error)> GetAllPOrganizations()
        {
            var organizations = await _context.POrganizations.ToListAsync();
            if (organizations == null || organizations.Count  == 0)
                return (null, "404"); // No organizations found

           return (organizations.Select(o => new OrgniztionResponseDTOs
            {
                Id = o.Id,
                Dscrp = o.Dscrp,
                DepartId = o.DepartId,
                AccountUnitId = o.AccountUnitId,
                BranchId = o.BranchId
            }).ToList(), null); // Return list of organizations
        }

        public async Task<(OrgniztionResponseDTOs? POrganization, string? error)> GetPOrganizationById(int Id)
        {
            var orgn = await _context.POrganizations.FirstOrDefaultAsync(o => o.Id == Id);
            if (orgn == null)
                return (null, "404"); // Organization not found

            return (new OrgniztionResponseDTOs
            {
                Id = orgn.Id,
                Dscrp = orgn.Dscrp,
                DepartId = orgn.DepartId,
                AccountUnitId = orgn.AccountUnitId,
                BranchId = orgn.BranchId
            }, null); // Return organization details
        }

        public async Task<(OrgniztionResponseDTOs? POrganization, string? error)> GetPOrganizationByDepartId(int Id)
        {
            var orgn = await _context.POrganizations.FirstOrDefaultAsync(o => o.DepartId == Id);
            if (orgn == null)
                return (null, "404"); // Organization not found

            return (new OrgniztionResponseDTOs
            {
                Id = orgn.Id,
                Dscrp = orgn.Dscrp,
                DepartId = orgn.DepartId,
                AccountUnitId = orgn.AccountUnitId,
                BranchId = orgn.BranchId
            }, null); // Return organization details
        }

        public async Task<string> DeletePOrganization(int Id)
        {
            var orgn = await _context.POrganizations.FirstOrDefaultAsync(o => o.Id == Id);
            if (orgn == null)
                return ("404"); // Organization not found

            _context.POrganizations.Remove(orgn);
            await _context.SaveChangesAsync();

            return "200"; // Organization Removed Successfully
        }

        // JobTitle Logic 

        public async Task<(JobTitleResponseDTOs? Job, string? error)> PostJobTitle(JobTitleViewForm req)
        {
            var job = await _context.PJobTitles.FirstOrDefaultAsync(j => j.Dscrp == req.Dscrp && j.StepId == req.StepId);
            if (job != null)
                return (null, "400"); // Job title already exists

            var newJob = new PJobTitle
            {
                Dscrp = req.Dscrp,
                StepId = req.StepId // Assuming StepId is nullable
            };

            _context.PJobTitles.Add(newJob);
            await _context.SaveChangesAsync();

            var response = new JobTitleResponseDTOs
            {
                Id = newJob.Id,
                Dscrp = newJob.Dscrp,
                StepId = newJob.StepId
            };

          return (response, null); // Success insert Job Title
        }

        public async Task<(JobTitleResponseDTOs? Job, string? error)> EditJobTitle(JobTitleViewForm req, int Id)
        {
           var job = await _context.PJobTitles.FirstOrDefaultAsync(j => j.Id == Id);
            if (job == null)
                return (null, "404"); // Job title not found
            // Check for duplicate job title (excluding current)
            var duplicate = await _context.PJobTitles
                .FirstOrDefaultAsync(j => j.Dscrp == req.Dscrp && j.StepId == req.StepId);
            if (duplicate != null)
                return (null, "400"); // Job title already exists
            // Update properties
            job.Dscrp = req.Dscrp;
            job.StepId = req.StepId; // Assuming StepId is nullable
            _context.PJobTitles.Update(job);
            await _context.SaveChangesAsync();
            var response = new JobTitleResponseDTOs
            {
                Id = job.Id,
                Dscrp = job.Dscrp,
                StepId = job.StepId
            };
            return (response, null); // Success update Job Title
        }

        public async Task<(List<JobTitleResponseDTOs>? Job, string? error)> GetAllJobTitle()
        {
            var jobs = await _context.PJobTitles.ToListAsync();
            if (jobs == null || jobs.Count == 0)
                return (null, "404"); // No job titles found

            return (jobs.Select(j => new JobTitleResponseDTOs
            {
                Id = j.Id,
                Dscrp = j.Dscrp,
                StepId = j.StepId
            }).ToList(), null); // Return list of job titles
        }

        public async Task<(JobTitleResponseDTOs? Job, string? error)> GetJobTitleById(int Id)
        {
            var jobs = await _context.PJobTitles.FirstOrDefaultAsync(j => j.Id == Id);
            if (jobs == null)
                return (null, "404"); // Job title not found

           if(jobs == null)
                return (null, "404"); // Job title not found
            var response = new JobTitleResponseDTOs
            {
                Id = jobs.Id,
                Dscrp = jobs.Dscrp,
                StepId = jobs.StepId
            };
            return (response, null); // Return job title details
        }

        public async Task<string> DeleteJobTitle(int Id)
        {
            var jobs = await _context.PJobTitles.FirstOrDefaultAsync(j => j.Id == Id);
            if (jobs == null)
                return ("404"); // Job title not found

            _context.PJobTitles.Remove(jobs);
            await _context.SaveChangesAsync();

            return "200"; // Job title Removed Successfully
        }

        
    }
}
