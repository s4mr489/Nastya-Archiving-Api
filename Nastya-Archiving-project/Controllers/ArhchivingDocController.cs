using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.JoinedDocs;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Services.archivingDocs;
using Nastya_Archiving_project.Services.SystemInfo;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArhchivingDocController : ControllerBase
    {
        private readonly IArchivingDocsSercvices _archivingDocsSercvices;
        private readonly ISystemInfoServices _systemInfoServcies;
        public ArhchivingDocController(IArchivingDocsSercvices archivingDocsSercvices, ISystemInfoServices systemInfoServcies)
        {
            _archivingDocsSercvices = archivingDocsSercvices;
            _systemInfoServcies = systemInfoServcies;
        }

        [HttpPost("Add-Docs-To-Archive")]
        public async Task<IActionResult> PostArchivingDocs([FromForm] FileViewForm file, [FromForm] ArchivingDocsViewForm req)
        {
            var (docs, error) = await _archivingDocsSercvices.PostArchivingDocs(req, file);
            if (error != null)
                return BadRequest(error);
            return Ok(docs);
        }

        /// <summary>
        /// Restore a deleted document by its Id.
        /// </summary>
        [HttpPost("Restore-Deleted-Docs")]
        public async Task<IActionResult> RestoreDeletedDocuments(int Id)
        {
            var result = await _archivingDocsSercvices.RestoreDeletedDocuments(Id);
            if (result == "404")
                return NotFound("Document not found.");
            return Ok("Document restored successfully.");
        }

        [HttpDelete("Remove-Docs")]
        public async Task<IActionResult> DeleteArchivingDoc(int id)
        {
            var result = await _archivingDocsSercvices.DeleteArchivingDocs(id);
            if (result == "404")
                return NotFound("Document not found.");
            return Ok("Document deleted successfully.");
        }

        [HttpGet("Get-Last-RefNo")]
        public async Task<IActionResult> GetLastRefNo()
        {
            var lastRefNo = await _systemInfoServcies.GetLastRefNo();
            return Ok(lastRefNo);
        }


        // <summary> This to edit the docs <summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> EditArchivingDoc(
        int id,
        [FromForm] ArchivingDocsViewForm req,
        [FromForm] FileViewForm? file)
        {
            var (docs, error) = await _archivingDocsSercvices.EditArchivingDocs(req, id, file);
            if (error != null)
                return BadRequest(new { error });

            return Ok(docs);
        }

        //<summary> This Implmention for joined the docs</summary>
        [HttpPost("join-docs-from-archive")]
        public async Task<IActionResult> JoinDocsFromArchive([FromBody] JoinedDocsViewForm req)
        {
            var result = await _archivingDocsSercvices.JoinDocsFromArchive(req);
            if (result.StatusCode == 404)
                return NotFound(result);

            return Ok(result);
        }
    }
}
