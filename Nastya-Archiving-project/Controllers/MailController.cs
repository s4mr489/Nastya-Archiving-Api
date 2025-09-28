using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.mail;
using Nastya_Archiving_project.Services.Mail;

namespace Nastya_Archiving_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MailController : ControllerBase
    {
        private readonly IMailServices _mailServices;

        public MailController(IMailServices mailServices)
        {
            _mailServices = mailServices;
        }

        /// <summary>
        /// Sends a mail to a recipient
        /// </summary>
        /// <param name="request">Mail details</param>
        [HttpPost("send")]
        public async Task<IActionResult> SendMail([FromBody] MailViewForm request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mailServices.SendMail(request);
            return GetActionResult(result);
        }

        /// <summary>
        /// Gets all mails for the current user and marks unread mails as read
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllMails()
        {
            var result = await _mailServices.GetAllMails();
            return GetActionResult(result);
        }

        /// <summary>
        /// Gets only unread mails for the current user
        /// </summary>
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadMails()
        {
            var result = await _mailServices.GetUnreadMails();
            return GetActionResult(result);
        }

        /// <summary>
        /// Marks a specific mail as read
        /// </summary>
        /// <param name="id">ID of the mail to mark as read</param>
        [HttpPut("read/{id:int}")]
        public async Task<IActionResult> MarkMailAsRead(int id)
        {
            var result = await _mailServices.MarkMailAsRead(id);
            return GetActionResult(result); 
        }

        /// <summary>
        /// Gets mail count statistics for the current user
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetMailCount()
        {
            var result = await _mailServices.GetMailCount();
            return GetActionResult(result);
        }
        
        /// <summary>
        /// Marks all mails as read in a single operation
        /// </summary>
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllMailsAsRead()
        {
            // This calls GetAllMails which marks all as read as a side effect
            var result = await _mailServices.GetAllMails();
            
            if (result.StatusCode == 200)
            {
                var responseData = result.Data as dynamic;
                var markedCount = responseData?.UnreadCount ?? 0;
                return Ok(new { MarkedAsRead = true, Count = markedCount, Message = "All mails marked as read" });
            }
            
            return GetActionResult(result);
        }

        /// <summary>
        /// filters mails based on specified criteria for the current user
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet("filtered-mails")]
        public async Task<IActionResult> GetFilteredMails([FromQuery] MailFilterViewForm filter)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _mailServices.GetFilteredMails(filter);
            return GetActionResult(result);
        }
        /// <summary>
        /// Helper method to convert BaseResponseDTOs to appropriate IActionResult
        /// </summary>
        private IActionResult GetActionResult(BaseResponseDTOs response)
        {
            return response.StatusCode switch
            {
                200 => Ok(response.Data),
                201 => StatusCode(201, response.Data),
                204 => NoContent(),
                400 => BadRequest(new { error = response.Error }),
                404 => NotFound(new { error = response.Error }),
                500 => StatusCode(500, new { error = response.Error }),
                _ => StatusCode(response.StatusCode, response.StatusCode >= 400 ? new { error = response.Error } : response.Data)
            };
        }
    }
}

