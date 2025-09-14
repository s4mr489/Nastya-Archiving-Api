using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Services.SystemInfo;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BackUpController : ControllerBase
    {
        private readonly ISystemInfoServices _systemInfoServices;
        private readonly AppDbContext _context;
        public BackUpController(ISystemInfoServices systemInfoServices, AppDbContext context)
        {
            _systemInfoServices = systemInfoServices;
            _context = context;
        }

        //[HttpPost("DataBase_BackUp")]
        //public async Task<IActionResult> DatabaseBackUP(string? PackUpPath)
        //{
        //    _systemInfoServices.BackupDatabase(PackUpPath);
        //    return Ok("Backup completed.");
        //}


        /// <summary>
        /// Exports all database data to CSV files and returns the ZIP archive for direct download
        /// </summary>
        /// <param name="exportDirectory">Directory path where the export files will be stored</param>
        /// <returns>Tuple containing success status, message, file path and file content for direct download</returns>
        [HttpPost("backup-all-databases-TO-Excel")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportDatabase(string exportDirectory)
        {
            var result = await _systemInfoServices.ExportAllDatabaseData(exportDirectory);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            // Return the file for direct download
            string fileName = Path.GetFileName(result.ExportFilePath);
            return File(result.FileContent, "application/zip", fileName);
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
        public async Task<IActionResult> BackupDatabase(string backupDirectory)
        {
            var result = await _systemInfoServices.CreateAdvancedDatabaseBackup(backupDirectory);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            // Return the file for download
            string fileName = Path.GetFileName(result.BackupFilePath);
            return File(result.FileContent, "application/octet-stream", fileName);
        }
        /// <summary>
        /// this endpoint used to backUp the files from the archive path to the user path
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="accountUnit"></param>
        /// <returns></returns>
        [HttpPost("Back-Up-Files-To-Path")]
        public async Task<BaseResponseDTOs> BackUpFiles(int archivePointId , bool allFiles =false)
        {
            var result = await _systemInfoServices.BackUpFiles(archivePointId, allFiles);
            return result;
        }

        [HttpGet("Get-Backup-path")]
        public async Task<IActionResult> GetBackupPath(int departId)
        {
            try
            {
                var backupPath = await _systemInfoServices.GetbackupPath(departId);
                if (backupPath == null)
                {
                    return NotFound(new { Error = "department  found" });
                }
                return Ok(new { BackupPath = backupPath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "An error occurred while retrieving the backup path",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("Get-Last-Four-Parttions-For-BackUp")]
        public async Task<IActionResult> BackUpParttions()
        {
            var result = await _systemInfoServices.GetLastFourPartitions();
            return StatusCode(result.StatusCode , result);
        }

        [HttpGet("Get-Storage-For-Arcive")]
        public async Task<IActionResult> Storage()
        {
            var result = await _systemInfoServices.GetIPartition();
            return StatusCode(result.StatusCode, result);
        }
        public class BackupRequest
        {
            public string BackupPath { get; set; } = "";
        }
    }
}
