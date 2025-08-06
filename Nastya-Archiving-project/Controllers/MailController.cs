using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.mail;
using Nastya_Archiving_project.Services.Mail;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly IMailServices _mailServices;

        public MailController(IMailServices mailServices)
        {
            _mailServices = mailServices;
        }

        [HttpPost("Send-Mail")]
        public async Task<IActionResult> SendMail(MailViewForm req)
        {
            var result = await _mailServices.SendMail(req);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gets all mails for the current user
        /// </summary>
        /// <returns>List of mail messages with associated documents</returns>
        [HttpGet("get-all")]
        public async Task<ActionResult<BaseResponseDTOs>> GetAllMails()
        {
            var result = await _mailServices.GetAllMails();

            return StatusCode(result.StatusCode, result);
        }
    }
}

