using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.AccountUnit;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Branch;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.GroupForm;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.JobTitle;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Organization;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.infrastructure;

namespace Nastya_Archiving_project.Controllers
{
    [Authorize(Roles = "Admin,Manager,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class InfrastructureController : ControllerBase
    {
        private readonly IInfrastructureServices _infrastructureServices;
        public InfrastructureController(IInfrastructureServices infrastructureService)
        {
            _infrastructureServices = infrastructureService;
        }

        // Account Unit Endpoints
        [HttpPost("Create-AccountUnit")]
        public async Task<IActionResult> PostAccountUnit([FromBody] AccountUnitViewForm req)
        {
            var (accountUnit, error) = await _infrastructureServices.PostAccountUint(req);
            if (error == "400")
                return BadRequest("Account unit already exists.");
            if (accountUnit == null)
                return StatusCode(500, "Unknown error.");
            return Ok(accountUnit);
        }

        [HttpGet("Get-AccountUnit/{id}")]
        public async Task<IActionResult> GetAccountUnitById(int id)
        {
            var (accountUnit, error) = await _infrastructureServices.GetAccountUintById(id);
            if (error == "404")
                return NotFound();
            return Ok(accountUnit);
        }

        [HttpGet("GetAll-AccountUnits")]
        public async Task<IActionResult> GetAllAccountUnits()
        {
            var (accountUnits, error) = await _infrastructureServices.GetAllAccountUint();
            if (error == "404")
                return NotFound();
            return Ok(accountUnits);
        }

        [HttpPut("Edit-AccountUnits/{id}")]
        public async Task<IActionResult> EditAccountUnit([FromBody] AccountUnitViewForm req, int id)
        {
            var (accountUnit, error) = await _infrastructureServices.EditAccountUint(req, id);
            if (error == "404")
                return NotFound();
            if (error == "400")
                return BadRequest("Duplicate account unit name.");
            return Ok(accountUnit);
        }

        [HttpDelete("Delete-AccountUnits/{id}")]
        public async Task<IActionResult> DeleteAccountUnit(int id)
        {
            var result = await _infrastructureServices.DeleteAccountUint(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }

        // Group Endpoints

        [HttpPost("Create-Groups")]
        public async Task<IActionResult> PostGroup([FromBody] GroupViewForm req)
        {
            var (group, error) = await _infrastructureServices.PostGroup(req);
            if (error == "400")
                return BadRequest("Group already exists.");
            if(error == "404")
                return NotFound("Account unit not found.");
            if (group == null)
                return StatusCode(400, error ?? "Unknown error.");
            return Ok(group);
        }

        [HttpPut("Edit-Groups/{id}")]
        public async Task<IActionResult> EditGroup([FromBody] GroupViewForm req, int id)
        {
            var (group, error) = await _infrastructureServices.EditGroup(req, id);
            if (error == "404")
                return NotFound();
            if (error == "400")
                return BadRequest("Duplicate group name.");
            return Ok(group);
        }

        [HttpGet("GetAll-Groups")]
        public async Task<IActionResult> GetAllGroups()
        {
            var (groups, error) = await _infrastructureServices.GetAllGroups();
            if (error == "404")
                return NotFound();
            return Ok(groups);
        }

        [HttpGet("Get-Groups/{id}")]
        public async Task<IActionResult> GetGroupById(int id)
        {
            var (group, error) = await _infrastructureServices.GetGrouptById(id);
            if (error == "404")
                return NotFound();
            return Ok(group);
        }

        [HttpDelete("Delete-Group/{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            var result = await _infrastructureServices.DeleteGroup(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }

        // Branch Endpoints

        [HttpPost("Create-Branch")]
        public async Task<IActionResult> PostBranch([FromBody] BranchViewForm req)
        {
            var (branch, error) = await _infrastructureServices.PostBranch(req);
            if (error == "400")
                return BadRequest("Branch name already exists.");
            if (error == "404")
                return NotFound("Account unit not found.");
            return Ok(branch);
        }

        [HttpPut("Edit-Branch/{id}")]
        public async Task<IActionResult> EditBranch([FromBody] BranchViewForm req, int id)
        {
            var (branch, error) = await _infrastructureServices.EditBranch(req, id);
            if (error == "404")
                return NotFound("Branch or account unit not found.");
            if (error == "400")
                return BadRequest("Duplicate branch name.");
            return Ok(branch);
        }

        [HttpGet("GetAll-Branches")]
        public async Task<IActionResult> GetAllBranches()
        {
            var (branches, error) = await _infrastructureServices.GetAllBranches();
            if (error == "404")
                return NotFound();
            return Ok(branches);
        }

        [HttpGet("Get-Branch/{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            var (branch, error) = await _infrastructureServices.GetBranchById(id);
            if (error == "404")
                return NotFound();
            return Ok(branch);
        }

        [HttpDelete("Delete-Branch/{id}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var result = await _infrastructureServices.DeleteBranch(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }

        // Department Endpoints

        [HttpPost("Create-Department")]
        public async Task<IActionResult> PostDepartment([FromBody] DepartmentViewForm req)
        {
            var (department, error) = await _infrastructureServices.PostDepartment(req);
            if (error == "400")
                return BadRequest("Department name already exists.");
            if (error == "404")
                return NotFound("Branch or account unit not found.");
            return Ok(department);
        }

        [HttpPut("Edit-Department/{id}")]
        public async Task<IActionResult> EditDepartment([FromBody] DepartmentViewForm req, int id)
        {
            var (department, error) = await _infrastructureServices.EditDeparment(req, id);
            if (error == "404")
                return NotFound("Branch or account unit not found.");
            if (error == "400")
                return BadRequest("Duplicate department name.");
            return Ok(department);
        }

        [HttpGet("GetAll-Departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var (departments, error) = await _infrastructureServices.GetAllDepartment();
            if (error == "404")
                return NotFound();
            return Ok(departments);
        }

        [HttpGet("Get-Department/{id}")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            var (department, error) = await _infrastructureServices.GetDepartmentById(id);
            if (error == "404")
                return NotFound();
            return Ok(department);
        }

        [HttpDelete("Delete-Department/{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var result = await _infrastructureServices.DeleteDepartment(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }

        //Jopb Title Endpoints
        [HttpPost("Create-JobTitle")]
        public async Task<IActionResult> PostJobTitle([FromBody] JobTitleViewForm req)
        {
            var (job, error) = await _infrastructureServices.PostJobTitle(req);
            if (error == "400")
                return BadRequest("Job title already exists.");
            return Ok(job);
        }

        [HttpPut("Edit-JobTitle/{id}")]
        public async Task<IActionResult> EditJobTitle([FromBody] JobTitleViewForm req, int id)
        {
            var (job, error) = await _infrastructureServices.EditJobTitle(req, id);
            if (error == "404")
                return NotFound("Job title not found.");
            if (error == "400")
                return BadRequest("Duplicate job title.");
            return Ok(job);
        }

        [HttpGet("GetAll-JobTitles")]
        public async Task<IActionResult> GetAllJobTitles()
        {
            var (jobs, error) = await _infrastructureServices.GetAllJobTitle();
            if (error == "404")
                return NotFound();
            return Ok(jobs);
        }

        [HttpGet("Get-JobTitle/{id}")]
        public async Task<IActionResult> GetJobTitleById(int id)
        {
            var (job, error) = await _infrastructureServices.GetJobTitleById(id);
            if (error == "404")
                return NotFound();
            return Ok(job);
        }

        [HttpDelete("Delete-JobTitle/{id}")]
        public async Task<IActionResult> DeleteJobTitle(int id)
        {
            var result = await _infrastructureServices.DeleteJobTitle(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }

        // Organization Endpoints

        [HttpPost("Create-Organization")]
        public async Task<IActionResult> PostPOrganization([FromBody] OrgniztionViewForm req)
        {
            var (org, error) = await _infrastructureServices.PostPOrganization(req);
            if (error == "400")
                return BadRequest("Organization already exists.");
            if (error == "404")
                return NotFound("Account unit, branch, or department not found.");
            return Ok(org);
        }

        [HttpPut("Edit-Organization/{id}")]
        public async Task<IActionResult> EditPOrganization([FromBody] OrgniztionViewForm req, int id)
        {
            var (org, error) = await _infrastructureServices.EditPOrganization(req, id);
            if (error == "404")
                return NotFound("Organization, account unit, branch, or department not found.");
            return Ok(org);
        }

        [HttpGet("GetAll-Organizations")]
        public async Task<IActionResult> GetAllPOrganizations()
        {
            var (orgs, error) = await _infrastructureServices.GetAllPOrganizations();
            if (error == "404")
                return NotFound();
            return Ok(orgs);
        }

        [HttpGet("Get-Organization/{id}")]
        public async Task<IActionResult> GetPOrganizationById(int id)
        {
            var (org, error) = await _infrastructureServices.GetPOrganizationById(id);
            if (error == "404")
                return NotFound();
            return Ok(org);
        }

        [HttpGet("Get-Organization-ByDepart/{id}")]
        public async Task<IActionResult> GetPOrganizationByDepartId(int id)
        {
            var (org, error) = await _infrastructureServices.GetPOrganizationByDepartId(id);
            if (error == "404")
                return NotFound();
            return Ok(org);
        }


        [HttpDelete("Delete-Organization/{id}")]
        public async Task<IActionResult> DeletePOrganization(int id)
        {
            var result = await _infrastructureServices.DeletePOrganization(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }
    }
}
