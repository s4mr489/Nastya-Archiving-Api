using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.UserInterface;
using Nastya_Archiving_project.Services.userInterface;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInterfacesController : ControllerBase
    {
        private readonly IUserInterfaceServices _userInterfaceServices;

        public UserInterfacesController(IUserInterfaceServices userInterfaceServices)
        {
            _userInterfaceServices = userInterfaceServices;
        }

        [HttpPost("Post-Interface")]
        public async Task<IActionResult> CreateUserInterface([FromBody] UserInterfaceViewForm request)
        {
            var result = await _userInterfaceServices.CreateUserInterface(request);
            if (result == "400")
                return BadRequest("This page already exists.");
            return Ok("User interface created successfully.");
        }

        [HttpGet("All-UserInterface")]
        public async Task<IActionResult> GetPageUrlsGroupedByOutputType()
        {
            var result = await _userInterfaceServices.GetPageUrlsGroupedByOutputType();
            return Ok(result);
        }
        [HttpGet("Group-pages/{Id}")]
        public async Task<IActionResult> GetGropuPagesById(int Id)
        {
            var result = await _userInterfaceServices.GetGropuPagesById(Id);
            if (result.StatusCode == 200)
                return Ok(result);
            return StatusCode(result.StatusCode , result);
        }

        [HttpGet("Group-pages")]
        public async Task<IActionResult> GetUserInterfaceForUser()
        {
            var (urls, error) = await _userInterfaceServices.GetUserInterfaceForUser();
            if (error == "401")
                return Unauthorized();
            if (error == "400")
                return BadRequest("Invalid user ID format.");
            if (error == "404")
                return NotFound("Group not found.");
            return Ok(urls);
        }
    }
}
