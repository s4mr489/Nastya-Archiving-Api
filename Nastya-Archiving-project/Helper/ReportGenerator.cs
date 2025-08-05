using DocumentFormat.OpenXml.Bibliography;
using FastReport;
using FastReport.Export.PdfSimple;
using Nastya_Archiving_project.Models.DTOs.Reports;

namespace Nastya_Archiving_project.Helper
{
    public class ReportGenerator
    {
        public byte[] GenerateUserDocumentsReport(List<DocumentDto> documents)
        {
            using var report = new Report();

            // Register data source
            report.RegisterData(documents, "Documents");

            // Enable nested access (optional)
            report.GetDataSource("Documents").Enabled = true;

            // Load the report layout (created in FastReport Designer)
            report.Load("Reports/UserDocumentsReport.frx");

            // Prepare report
            report.Prepare();

            using MemoryStream ms = new();
            PDFSimpleExport pdfExport = new();
            report.Export(pdfExport, ms);
            return ms.ToArray();
        }
    }
}
