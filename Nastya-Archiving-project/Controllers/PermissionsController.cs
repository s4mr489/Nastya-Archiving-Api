using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Services.Permmsions;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionsServices _permissionsServices;

        public PermissionsController(IPermissionsServices permissionsServices)
        {
            _permissionsServices = permissionsServices;
        }

        [HttpPost("add-group-permissions")]
        public async Task<IActionResult> AddGroupPermissions([FromQuery] int groupId, [FromBody] List<int> permissionsIds)
        {
            var result = await _permissionsServices.AddGroupPermissions(groupId, permissionsIds);
            if (result == "404")
                return NotFound("Group or permissions not found.");
            return Ok("Permissions added successfully.");
        }

        [HttpDelete("delete-group-permissions")]
        public async Task<IActionResult> DeleteGroupPermissions([FromQuery] int groupId, [FromBody] List<int> permissionsIds)
        {
            var result = await _permissionsServices.DeletedGroupPermissions(groupId, permissionsIds);
            if (result == "404")
                return NotFound("Group or permissions not found.");
            return Ok("Permissions deleted successfully.");
        }

        [HttpGet("group-permissions/{groupId}")]
        public async Task<IActionResult> GetPermissionsByGroupId(int groupId)
        {
            var (permissions, error) = await _permissionsServices.GetPermissionsByGroupId(groupId);
            if (error == "404")
                return NotFound("Group or permissions not found.");
            return Ok(permissions);
        }

        [HttpGet("all-permission")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _permissionsServices.GetAllPermissions();
            if (permissions.StatusCode == 404)
                return NotFound("No permissions found.");
            return StatusCode(permissions.StatusCode, permissions);
        }

        [HttpPost("copy-user-permission-for-group")]
        public async Task<IActionResult> CopyUserPermissionForGroup([FromQuery] int Id)
        {
            var result = await _permissionsServices.CopyUserPermissionForGroup(Id);
            if (result.StatusCode == 404)
                return NotFound("User or group not found.");
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("copy-user-permission-to-multiple-users")]
        public async Task<IActionResult> CopyUserPermissionToMultipleUsers([FromQuery] int sourceUserId, [FromBody] List<int> targetUserIds)
        {
            var result = await _permissionsServices.CopyUserPermissionToMultipleUsers(sourceUserId, targetUserIds);
            if (result.StatusCode == 404)
                return NotFound("Source user or target users not found.");
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("Copy-Group-permissions-To-Users")]
        public async Task<IActionResult> CopyGroupPermissionsToUsersAsync(int Id)
        {
            var result = await _permissionsServices.CopyGroupPermissionsToUsersAsync(Id);
            if (result.StatusCode == 404)
                return NotFound("User or permissions not found.");
            return StatusCode(result.StatusCode, result);
        }
    }
}
