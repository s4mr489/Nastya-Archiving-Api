using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.mail;

namespace Nastya_Archiving_project.Services.Mail
{
    public interface IMailServices
    {
        Task<BaseResponseDTOs> SendMail(MailViewForm req);
        Task<BaseResponseDTOs> GetAllMails();
        Task<BaseResponseDTOs> GetMailList(MailResponseDTOs result);
    }
}
