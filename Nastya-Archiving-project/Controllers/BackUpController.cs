using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Services.SystemInfo;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BackUpController : ControllerBase
    {
        private readonly ISystemInfoServices _systemInfoServices;
        public BackUpController(ISystemInfoServices systemInfoServices)
        {
            _systemInfoServices = systemInfoServices;
        }

        [HttpPost("DataBase_BackUp")]
        public async Task<IActionResult> DatabaseBackUP(string? PackUpPath)
        {
            _systemInfoServices.BackupDatabase(PackUpPath);
            return Ok("Backup completed.");
        }

        [HttpPost("backup")]
        public IActionResult BackupDatabase([FromQuery] string backupDirectory)
        {
            try
            {
                _systemInfoServices.BackupDatabase(backupDirectory);
                return Ok(new { message = "Backup completed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
