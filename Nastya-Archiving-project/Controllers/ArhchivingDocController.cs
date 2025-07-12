using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
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
        public async Task<IActionResult> PostArchivingDocs([FromBody] ArchivingDocsViewForm req)
        {
            var (docs, error) = await _archivingDocsSercvices.PostArchivingDocs(req);
            if (error != null)
                return BadRequest(error);
            return Ok(docs);
        }
    }
}
