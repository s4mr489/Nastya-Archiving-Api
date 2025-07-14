using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
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
            return Ok(user);
        }


        //that should remove when i done from it because it's danger 
        [HttpGet("test")]
        public async Task<IActionResult> Test(string username , string password)
        {
            var users = await _context.Users.ToListAsync();

            // Create a projection with decrypted password, do not save to DB
            var result = users.Select(user => new
            {
                user.Id,
                user.Realname,
                DecryptedUserName = _encryptionServices.DecryptString256Bit(user.UserName),
                DecryptedPassword = _encryptionServices.DecryptString256Bit(user.UserPassword),
                user.GroupId,       
                DecryptedPermtype =_encryptionServices.DecryptString256Bit(user.Permtype),
                DecryptedAdminst = _encryptionServices.DecryptString256Bit(user.Adminst),
                user.Editor,
                user.EditDate,
                user.AccountUnitId,
                user.GobStep,
                user.DepariId,
                user.DevisionId,
                user.BranchId,
                user.AsWfuser,
                user.AsmailCenter,
                user.JobTitle
            }).ToList();

            return Ok(result);

        }
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

        [HttpPut("edit-user/{id}")]
        public async Task<IActionResult> EditUser(int id, [FromBody] RegisterViewForm form, [FromQuery] bool isAdmin = false)
        {
            var (user, error) = await _authServices.EditUser(id, form, isAdmin);
            if (error != null)
                return BadRequest(new { error });
            return Ok(user);
        }

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var (users, error) = await _authServices.GetAllUsers(pageNumber, pageSize);
            if (error != null)
                return NotFound(error);
            return Ok(users);
        }

        [HttpDelete("remove-user/{id}")]
        public async Task<IActionResult> RemoveUser(int id)
        {
            var result = await _authServices.RemoveUser(id);
            if (result == "400")
                return NotFound(new { error = "User not found." });
            return Ok(new { message = "User removed successfully." });
        }

        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? realName, [FromQuery] string? userName)
        {
            var (user, error) = await _authServices.SearchUsers(realName, userName);
            if (error != null)
                return NotFound(new { error });
            return Ok(user);
        }

    }
}
