using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Models.DTOs.UserPermission;
using Nastya_Archiving_project.Services.usersPermission;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersPermissionController : ControllerBase
    {
        private readonly IUserPermissionsServices _userPermissionsServices;

        public UsersPermissionController(IUserPermissionsServices userPermissionsServices)
        {
            _userPermissionsServices = userPermissionsServices;
        }

        [HttpGet("get-users")]
        public async Task<ActionResult<List<UsersSearchResponseDTOs>>> GetUsers([FromQuery] UsersSearchViewForm search)
        {
            var result = await _userPermissionsServices.GetUsersAsync(search);
            return Ok(result);
        }

        [HttpGet("get-user-permissions")]
        public async Task<ActionResult<BaseResponseDTOs>> GetUserPermissions([FromQuery] UsersViewForm users)
        {
            var result = await _userPermissionsServices.GetUserPermissionsAsync(users);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("create-user-permissions")]
        public async Task<ActionResult<BaseResponseDTOs>> CreateUserPermissions([FromBody] CreateUserPermissionsRequestDTO request)
        {
            var result = await _userPermissionsServices.CreateUserPermissionsAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("delete-user-permissions")]
        public async Task<ActionResult<BaseResponseDTOs>> DeleteUserPermissions([FromBody] CreateUserPermissionsRequestDTO request)
        {
            var result = await _userPermissionsServices.DeleteUserPermissionsAsync(request);
            return StatusCode(result.StatusCode, result);
        }
    }
}
