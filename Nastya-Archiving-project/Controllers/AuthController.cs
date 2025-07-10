using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.Auth;
using Nastya_Archiving_project.Services.auth;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthServices _authServices;
        public AuthController(IAuthServices authServices)
        {
            _authServices = authServices;
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
                return BadRequest("Incorrect password.");
            }
            else if (result == "403")
            {
                return Forbid("Access denied You Don't Have The Permmsions.");
            }
            else if (result == "500")
            {
                return StatusCode(500, "Internal server error.");
            }
            return Ok(new { Token = result });
        }

    }
}
