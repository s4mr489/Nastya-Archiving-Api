using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Services.archivingDocs;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArhchivingDocController : ControllerBase
    {
        private readonly IArchivingDocsSercvices _archivingDocsSercvices;
        public ArhchivingDocController(IArchivingDocsSercvices archivingDocsSercvices)
        {
            _archivingDocsSercvices = archivingDocsSercvices;
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
    }
}
