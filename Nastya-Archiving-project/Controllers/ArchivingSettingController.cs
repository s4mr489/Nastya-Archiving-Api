using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.ArchivingPoint;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.DocsType;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.Precedence;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.SupDocsType;
using Nastya_Archiving_project.Services.archivingDocs;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.encrpytion;

namespace Nastya_Archiving_project.Controllers
{
    [Authorize(Roles = "Admin,User")]
    [Route("api/[controller]")]
    [ApiController]
    public class ArchivingSettingController : ControllerBase
    {
        private readonly IArchivingSettingsServicers _archivingSettings;
        public ArchivingSettingController(IArchivingSettingsServicers archivingSettings, AppDbContext context, IEncryptionServices encryptionServices)
        {
            _archivingSettings = archivingSettings;
        }
        // Archiving Point Endpoints

        [HttpPost("Create-ArchivingPoint")]
        public async Task<IActionResult> PostArchivingPoint([FromBody] ArchivingPointViewForm req)
        {
            var (point, error) = await _archivingSettings.PostArchivingPoint(req);
            if (error == "400")
                return BadRequest("Archiving point already exists or invalid paths.");
            if (error == "404")
                return NotFound("Related entity not found.");
            return Ok(point);
        }

        [HttpPut("Edit-ArchivingPoint/{id}")]
        public async Task<IActionResult> EditArchivingPoint([FromBody] ArchivingPointViewForm req, int id)
        {
            var (point, error) = await _archivingSettings.EditArchivingPoint(req, id);
            if (error == "404")
                return NotFound("Archiving point or related entity not found.");
            if (error == "400")
                return BadRequest("Duplicate archiving point or invalid paths.");
            return Ok(point);
        }

        [HttpGet("GetAll-ArchivingPoints")]
        public async Task<IActionResult> GetAllArchivingPoints()
        {
            var (points, error) = await _archivingSettings.GetAllArchivingPoints();
            if (error == "404")
                return NotFound();
            return Ok(points);
        }

        [HttpGet("Get-ArchivingPoint/{id}")]
        public async Task<IActionResult> GetArchivingPointById(int id)
        {
            var (point, error) = await _archivingSettings.GetArchivingPointById(id);
            if (error == "404")
                return NotFound();
            return Ok(point);
        }

        [HttpDelete("Delete-ArchivingPoint/{id}")]
        public async Task<IActionResult> DeleteArchivingPoint(int id)
        {
            var result = await _archivingSettings.DeleteArchivingPoint(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }

        // DocsType Endpoints

        [HttpPost("Create-DocsType")]
        public async Task<IActionResult> PostDocsType([FromBody] DocTypeViewform req)
        {
            var (docsType, error) = await _archivingSettings.PostDocsType(req);
            if (error == "400")
                return BadRequest("Document type already exists.");
            return Ok(docsType);
        }

        [HttpPut("Edit-DocsType/{id}")]
        public async Task<IActionResult> EditDocsType([FromBody] DocTypeViewform req, int id)
        {
            var (docsType, error) = await _archivingSettings.EditDocsType(req, id);
            if (error == "404")
                return NotFound("Document type not found.");
            if (error == "400")
                return BadRequest("Duplicate document type.");
            return Ok(docsType);
        }

        [HttpGet("GetAll-DocsTypes")]
        public async Task<IActionResult> GetAllDocsTypes()
        {
            var (docsTypes, error) = await _archivingSettings.GetAllDocsTypes();
            if (error == "404")
                return NotFound();
            return Ok(docsTypes);
        }

        [HttpGet("Get-DocsType/{id}")]
        public async Task<IActionResult> GetDocsTypeById(int id)
        {
            var (docsType, error) = await _archivingSettings.GetDocsTypeById(id);
            if (error == "404")
                return NotFound();
            return Ok(docsType);
        }

        [HttpDelete("Delete-DocsType/{id}")]
        public async Task<IActionResult> DeleteDocsType(int id)
        {
            var result = await _archivingSettings.DeleteDocsType(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }

        // SupDocsType Endpoints

        [HttpPost("Create-SupDocsType")]
        public async Task<IActionResult> PostSupDocsType([FromBody] SupDocsTypeViewform req)
        {
            var (supDocsType, error) = await _archivingSettings.PostSupDocsType(req);
            if (error == "400")
                return BadRequest("SupDocsType already exists.");
            if (error == "404")
                return NotFound("Document type not found.");
            return Ok(supDocsType);
        }

        [HttpPut("Edit-SupDocsType/{id}")]
        public async Task<IActionResult> EditSupDocsType([FromBody] SupDocsTypeViewform req, int id)
        {
            var (supDocsType, error) = await _archivingSettings.EditSupDocsType(req, id);
            if (error == "404")
                return NotFound("SupDocsType or document type not found.");
            if (error == "400")
                return BadRequest("Duplicate SupDocsType.");
            return Ok(supDocsType);
        }

        [HttpGet("GetAll-SupDocsTypes")]
        public async Task<IActionResult> GetAllSupDocsTypes()
        {
            var (supDocsTypes, error) = await _archivingSettings.GetAllSupDocsTypes();
            if (error == "404")
                return NotFound();
            return Ok(supDocsTypes);
        }

        [HttpGet("Get-SupDocsType/{id}")]
        public async Task<IActionResult> GetSupDocsTypeById(int id)
        {
            var (supDocsType, error) = await _archivingSettings.GetSupDocsTypeById(id);
            if (error == "404")
                return NotFound();
            return Ok(supDocsType);
        }

        [HttpDelete("Delete-SupDocsType/{id}")]
        public async Task<IActionResult> DeleteSupDocsType(int id)
        {
            var result = await _archivingSettings.DeleteSupDocsType(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }

        // Precedence Endpoints

        [HttpPost("Create-Precedence")]
        public async Task<IActionResult> PostPrecednce([FromBody] PrecedenceViewForm req)
        {
            var (precedence, error) = await _archivingSettings.PostPrecednce(req);
            if (error == "400")
                return BadRequest("Precedence already exists.");
            return Ok(precedence);
        }

        [HttpPut("Edit-Precedence/{id}")]
        public async Task<IActionResult> EditPrecednce([FromBody] PrecedenceViewForm req, int id)
        {
            var (precedence, error) = await _archivingSettings.EditPrecednce(req, id);
            if (error == "404")
                return NotFound("Precedence not found.");
            if (error == "400")
                return BadRequest("Duplicate precedence.");
            return Ok(precedence);
        }

        [HttpGet("GetAll-Precedences")]
        public async Task<IActionResult> GetAllPrecednces()
        {
            var (precedences, error) = await _archivingSettings.GetAllPrecednces();
            if (error == "404")
                return NotFound();
            return Ok(precedences);
        }

        [HttpGet("Get-Precedence/{id}")]
        public async Task<IActionResult> GetPrecednceById(int id)
        {
            var (precedence, error) = await _archivingSettings.GetPrecednceById(id);
            if (error == "404")
                return NotFound();
            return Ok(precedence);
        }

        [HttpDelete("Delete-Precedence/{id}")]
        public async Task<IActionResult> DeletePrecednce(int id)
        {
            var result = await _archivingSettings.DeletePrecednce(id);
            if (result == "404")
                return NotFound();
            return Ok(result);
        }
    }
}
