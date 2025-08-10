namespace Nastya_Archiving_project.Models.DTOs.mail
{
    public class MailViewForm
    {
        /// <summary>
        /// Reference number of document to attach to the mail
        /// </summary>
        public string? ReferenceNo { get; set; }
        public string? from { get; set; }

        // Can be a single recipient or comma/semicolon separated list
        public string? to { get; set; }

        // Explicit list of recipients (preferred way when sending to multiple users)
        public List<string>? recipients { get; set; }

        public string? Notes { get; set; }
    }
}
