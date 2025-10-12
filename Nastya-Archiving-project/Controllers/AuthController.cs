using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Auth;
using Nastya_Archiving_project.Services.auth;
using Nastya_Archiving_project.Services.encrpytion;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuthServices _authServices;
        private readonly IEncryptionServices _encryptionServices;
        public AuthController(IAuthServices authServices, IEncryptionServices encryptionServices, AppDbContext context)
        {
            _authServices = authServices;
            _encryptionServices = encryptionServices;
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]LoginFormDTO form, bool IsAdmin = false)
        {
            if (form == null || string.IsNullOrEmpty(form.userName) || string.IsNullOrEmpty(form.password))
            {
                return BadRequest("Invalid login credentials.");
            }
            var result = await _authServices.Login(form, IsAdmin);
            if (result == "404")
            {
                return NotFound("User not found.");
            }
            else if (result == "400")
            {
                return BadRequest("Incorrect username or password.");
            }
            else if (result == "403")
            {
                return Forbid();
            }
            else if (result == "500")
            {
                return StatusCode(500, "Internal server error.");
            }
            return Ok(new { Token = result });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Register-User-ByAdmin")]
        public async Task<IActionResult> Register([FromBody] RegisterViewForm form, [FromQuery] bool isAdmin = false)
        {
            var (user, error) = await _authServices.Register(form, isAdmin);
            if (error != null)
                return BadRequest(new { error });
            if(error == "415")
                return Conflict(new { error = "Maximum user limit reached. Please contact your system administrator." });
            return Ok(user);
        }


        ////that should remove when i done from it because it's danger 
        //[HttpGet("test")]
        //public async Task<IActionResult> Test(
        //    )
        //{
        //    var users = await _context.Users.ToListAsync();

        //    // Create a projection with decrypted password, do not save to DB
        //    var result = users.Select(user => new
        //    {
        //        user.Id,
        //        RealName = user.Realname,
        //        // Add null checks before decryption
        //        DecryptedUserName = user.UserName != null ? _encryptionServices.DecryptString256Bit(user.UserName) : null,
        //        DecryptedPassword = user.UserPassword != null ? _encryptionServices.DecryptString256Bit(user.UserPassword) : null,
        //        user.GroupId,
        //        DecryptedPermtype = user.Permtype != null ? _encryptionServices.DecryptString256Bit(user.Permtype) : null,
        //        DecryptedAdminst = user.Adminst != null ? _encryptionServices.DecryptString256Bit(user.Adminst) : null,
        //        user.Editor,
        //        user.EditDate,
        //        user.AccountUnitId,
        //        user.GobStep,
        //        user.DepariId,
        //        user.DevisionId,
        //        user.BranchId,
        //        user.AsWfuser,
        //        user.AsmailCenter,
        //        user.JobTitle,
        //        user.Stoped
        //    }).ToList();

        //    return Ok(result);

        //}


        [HttpGet("ShowRegistered-Users")]
        public async Task<IActionResult> ShowRegisteredUsers()
        {
            var users = await _context.Users.CountAsync();
            if(users == 0 )
            {
                return Ok(new { ShowRegister = "False" });
            }
            return Ok(new { ShowRegister = "True" });
        }

        [HttpPost("Create-First-User")]
        public async Task<IActionResult> FirstUser(LoginFormDTO req)
        {
            BaseResponseDTOs result;
            result = await _authServices.FirstUsers(req);
            return StatusCode(result.StatusCode, result);

        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangeUserPassword([FromBody] ChangePasswordViewFrom pass)
        {
            var result = await _authServices.ChangeUserPassword(pass);
            return result switch
            {
                "404" => NotFound(new { error = "User not found." }),
                "400" => BadRequest(new { error = "Current password is incorrect." }),
                "200" => Ok(new { message = "Password changed successfully." }),
                _ => StatusCode(500, new { error = "Unknown error." })
            };
        }


        [Authorize]
        [HttpPut("edit-user/{id}")]
        public async Task<IActionResult> EditUser(int id, [FromBody] RegisterViewForm form, [FromQuery] bool isAdmin = false)
        {
            var (user, error) = await _authServices.EditUser(id, form, isAdmin);
            if (error != null)
                return BadRequest(new { error });
            return Ok(user);
        }


        [Authorize]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers([FromQuery]string? realName ,[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var (users, error) = await _authServices.GetAllUsers( realName , pageNumber, pageSize);
            if (error != null)
                return NotFound(error);
            return Ok(users);
        }


        [Authorize]
        [HttpDelete("remove-user/{id}")]
        public async Task<IActionResult> RemoveUser(int id)
        {
            var result = await _authServices.RemoveUser(id);
            if (result == "400")
                return NotFound(new { error = "User not found." });
            return Ok(new { message = "User removed successfully." });
        }


        [Authorize]
        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? realName, [FromQuery] int departId = 0, [FromQuery] string? userName = null)
        {
            var (user, error) = await _authServices.SearchUsers(realName, departId, userName);
            if (error != null)
                return NotFound(new { error });
            return Ok(user);
        }


        [Authorize]
        [HttpGet("Depart-for-user/{userId}")]
        public async Task<IActionResult> GetDepartForUsers(int userId)
        {
            var response = await _authServices.GetDepartForUsers(userId);
            if (response == null || response.StatusCode == 404)
                return NotFound(response);

            return Ok(response);
        }


        [Authorize]
        [HttpPatch("active-or-deactiv-user/{id}")]
        public async Task<IActionResult> ActiveOrDeActivUser(int id, [FromQuery] bool status)
        {
            var result = await _authServices.ActiveOrDeActivUser(id, status);
            return StatusCode(result.StatusCode, result);
        }


    }
}
