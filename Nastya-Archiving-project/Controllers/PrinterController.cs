using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Middleware;
using Nastya_Archiving_project.Services.printer;
using System.Text;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrinterController : ControllerBase
    {
        private readonly IPrinterServices _printerServices;

        public PrinterController(IPrinterServices printerServices)
        {
            _printerServices = printerServices;
        }

        [HttpGet("printers")]
        public ActionResult<IEnumerable<PrinterInfo>> GetPrinters()
        {
            return Ok(_printerServices.GetAvailablePrinters());
        }

        [HttpPost("print/{printerId}")]
        public async Task<IActionResult> Print(string printerId, [FromBody] PrintRequest request)
        {
            if (string.IsNullOrEmpty(request.Content))
            {
                return BadRequest("Content is required");
            }

            // For a simple text document
            byte[] documentData;

            if (request.IsBase64)
            {
                documentData = Convert.FromBase64String(request.Content);
            }
            else
            {
                // Create a simple text or HTML document
                var htmlContent = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Print Document</title>
                        <style>
                            body {{ font-family: Arial, sans-serif; }}
                            h1 {{ color: #333; }}
                        </style>
                    </head>
                    <body>
                        <h1>{request.Title ?? "Print Document"}</h1>
                        <div>{request.Content}</div>
                    </body>
                    </html>
                ";

                documentData = Encoding.UTF8.GetBytes(htmlContent);
            }

            var settings = new Dictionary<string, string>
            {
                ["title"] = request.Title ?? "Print Document",
                ["contentType"] = request.IsBase64 ? (request.ContentType ?? "application/pdf") : "text/html",
                ["copies"] = (request.Copies ?? 1).ToString()
            };

            var success = await _printerServices.PrintDocument(printerId, documentData, settings);

            if (success)
            {
                return Ok(new { message = "Document sent to printer" });
            }
            else
            {
                return StatusCode(500, new { message = "Failed to print document" });
            }
        }
    }

    public class PrintRequest
    {
        public string? Title { get; set; }
        public string Content { get; set; } = "";
        public bool IsBase64 { get; set; }
        public string? ContentType { get; set; }
        public int? Copies { get; set; }
    }
}
