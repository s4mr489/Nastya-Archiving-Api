using Microsoft.AspNetCore.Authorization;
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

        //[HttpPost("DataBase_BackUp")]
        //public async Task<IActionResult> DatabaseBackUP(string? PackUpPath)
        //{
        //    _systemInfoServices.BackupDatabase(PackUpPath);
        //    return Ok("Backup completed.");
        //}


        /// <summary>
        /// Creates a backup of the database
        /// </summary>
        /// <param name="backupPath">Directory path where the backup will be stored</param>
        /// <returns>Result of the backup operation</returns>
        [HttpPost("backup-all-databases")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BackupAllDatabases([FromBody] BackupRequest request)
        {
            if (string.IsNullOrEmpty(request.BackupPath))
            {
                return BadRequest(new { Error = "Backup path is required" });
            }

            try
            {
                var (success, message, backupFiles) = await _systemInfoServices.ExportAllDatabaseData(request.BackupPath);

                if (success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = message,
                        BackupFiles = backupFiles,
                        DatabaseCount = backupFiles.Count,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(500, new { Success = false, Error = message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = "An unexpected error occurred while backing up databases",
                    Details = ex.Message
                });
            }
        }


        /// <summary>
        /// Creates a full database backup with advanced options
        /// </summary>
        /// <returns>Result of the backup operation with file path</returns>
        [HttpPost("advanced-backup")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAdvancedDatabaseBackup([FromBody]BackupRequest request)
        {
            try
            {
                var (success, message, backupFilePath) = await _systemInfoServices.CreateAdvancedDatabaseBackup(
                    request.BackupPath
                );

                if (success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = message,
                        BackupFilePath = backupFilePath,
                        Timestamp = DateTime.UtcNow
                    });

                }
                else
                {
                    return StatusCode(500, new { Success = false, Error = message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Error = "An unexpected error occurred while backing up the database",
                    Details = ex.Message
                });
            }
        }
        public class BackupRequest
        {
            public string BackupPath { get; set; } = "";
        }
    }
}
