namespace Nastya_Archiving_project.Models.DTOs.mail
{
    public class MailResponseDTOs
    {
        public string? filePath { get; set; }
        public object? docc { get; set; }
        public string? referenceNo { get; set; }
        public string? docNo { get; set; }
        public DateOnly? docDate { get; set; }
        public string? supDocType { get; set; }
        public string? subject { get; set; }
        public string? notes { get; set; }
        public string? source { get; set; }
        public string? target { get; set; }
        public string? sender { get; set; }
    }
}
