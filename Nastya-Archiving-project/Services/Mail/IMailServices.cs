using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.mail;

namespace Nastya_Archiving_project.Services.Mail
{
    /// <summary>
    /// Interface for mail-related operations with real-time notification capabilities
    /// </summary>
    public interface IMailServices
    {
        /// <summary>
        /// Sends a mail to a recipient with optional document reference and real-time notification
        /// </summary>
        /// <param name="req">Mail details including recipient and document reference</param>
        /// <returns>Response with mail details or error information</returns>
        Task<BaseResponseDTOs> SendMail(MailViewForm req);

        /// <summary>
        /// Gets all mails (both read and unread) for the current user
        /// and marks unread mails as read
        /// </summary>
        /// <returns>Response with mail list and associated documents</returns>
        Task<BaseResponseDTOs> GetAllMails();

        /// <summary>
        /// Gets only unread mails for the current user without changing read status
        /// </summary>
        /// <returns>Response with unread mail list and associated documents</returns>
        Task<BaseResponseDTOs> GetUnreadMails();

        /// <summary>
        /// Marks a specific mail as read with real-time notification
        /// </summary>
        /// <param name="mailId">ID of the mail to mark as read</param>
        /// <returns>Response with updated mail read status</returns>
        Task<BaseResponseDTOs> MarkMailAsRead(int mailId);

        /// <summary>
        /// Gets mail count statistics for the current user
        /// </summary>
        /// <returns>Response with total and unread mail counts</returns>
        Task<BaseResponseDTOs> GetMailCount();

    }
}