using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.Limitation;
using Nastya_Archiving_project.Services.home;
using Nastya_Archiving_project.Services.Limitation;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LimiationsController : ControllerBase
    {
        private readonly ILimitationServices _limitationServices;
        public LimiationsController(ILimitationServices limitationServices)
        {
            _limitationServices = limitationServices;
        }



        /// <summary>
        /// Creates an encrypted text file containing system license information
        /// </summary>
        /// <returns>Status and file path information</returns>
        [HttpPost("create-encrypted-text")]
        public async Task<IActionResult> CreateEncryptedText([FromBody] LicenseCreationDTO req)
        {
            var result = await _limitationServices.CreateEncryptedTextFile(req);
            if (result.StatusCode == 200)
                return Ok(result);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Reads and decrypts a text file containing system license information
        /// </summary>
        /// <param name="path">Path to the encrypted text file</param>
        /// <returns>Decrypted license information</returns>
        [HttpGet("read-encrypted-text")]
        public async Task<IActionResult> ReadEncryptedText()
        {
            var result = await _limitationServices.ReadEncryptedTextFile();
            if (result.StatusCode == 200)
                return Ok(result);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets the current status of the smart search feature
        /// </summary>
        /// <returns>Status information indicating if smart search is enabled</returns>
        [HttpGet("smart-search/status")]
        public async Task<IActionResult> GetSmartSearchStatus()
        {
            var result = await _limitationServices.GetSmartSearchStatus();
            if (result.StatusCode == 200)
                return Ok(result);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Enables the smart search feature
        /// </summary>
        /// <returns>Status information after enabling smart search</returns>
        [HttpPost("smart-search/enable")]
        public async Task<IActionResult> EnableSmartSearch()
        {
            var result = await _limitationServices.EnableSmartSearch();
            if (result.StatusCode == 200)
                return Ok(result);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Disables the smart search feature
        /// </summary>
        /// <returns>Status information after disabling smart search</returns>
        [HttpPost("smart-search/disable")]
        public async Task<IActionResult> DisableSmartSearch()
        {
            var result = await _limitationServices.DisableSmartSearch();
            if (result.StatusCode == 200)
                return Ok(result);
            return StatusCode(result.StatusCode, result);
        }
    }
}
