namespace Nastya_Archiving_project.Models.DTOs.mail
{
    /// <summary>
    /// Filter class for mail queries
    /// </summary>
    public class MailFilterViewForm
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Sender { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Content { get; set; }
        public bool? IsRead { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
