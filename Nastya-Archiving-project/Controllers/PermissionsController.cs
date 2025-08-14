using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Services.Permmsions;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
    }
}
