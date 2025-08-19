using AspNetCore.Reporting;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Nastya_Archiving_project.Services.rdlcReport
{
    public sealed class RdlcReportServices : IRdlcReportServices
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public RdlcReportServices(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;

            // Needed by AspNetCore.Reporting for PDF/Word/Excel renderings
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task<byte[]> GenerateReportAsync(string reportName, string reportType, ReportFilter filter, CancellationToken ct = default)
        {
            // <project root>/Reports/<reportName>.rdlc (Recommended)
            var rdlcPath = Path.Combine(_env.ContentRootPath, "Report", $"{reportName}.rdlc");
            if (!System.IO.File.Exists(rdlcPath))
                throw new FileNotFoundException($"RDLC not found at: {rdlcPath}");

            // Query + filter
            var docs = await GetArchivingDocumentsForReport(filter, ct);

            // The name below ("ArcivingDocs") must match the RDLC DataSet/DataSource name exactly
            var localReport = new LocalReport(rdlcPath);
            localReport.AddDataSource("ArcivingDocs", docs);

            // RDLC parameters (optional)
            var parameters = new Dictionary<string, string>
            {
                ["ReportTitle"] = string.IsNullOrWhiteSpace(filter?.Title) ? "Document List Report" : filter.Title!,
                ["GeneratedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var render = GetRenderType(reportType);
            var result = localReport.Execute(render, 1, parameters);

            return result.MainStream; // bytes
        }

        private async Task<List<ArcivingDoc>> GetArchivingDocumentsForReport(ReportFilter? f, CancellationToken ct)
        {
            var q = _db.ArcivingDocs.AsNoTracking().AsQueryable();

            if (f?.FromDate is DateTime from) q = q.Where(d => d.EditDate >= from);
            if (f?.ToDate is DateTime to) q = q.Where(d => d.EditDate <= to);
            if (!string.IsNullOrWhiteSpace(f?.Subject)) q = q.Where(d => d.Subject.Contains(f.Subject!));
            if (!string.IsNullOrWhiteSpace(f?.Editor)) q = q.Where(d => d.Editor == f.Editor);

            if (f?.Ids?.Any() == true) q = q.Where(d => f.Ids!.Contains(d.Id));

            q = q.OrderByDescending(d => d.EditDate);

            if (f?.Take is int take && take > 0) q = q.Take(take);
            else q = q.Take(500); // sensible cap

            // Only select what the RDLC needs
            return await q.Select(d => new ArcivingDoc
            {
                Id = d.Id,
                RefrenceNo = d.RefrenceNo,
                DocNo = d.DocNo,
                DocDate = d.DocDate,
                Subject = d.Subject,
                Editor = d.Editor,
                EditDate = d.EditDate,
                ImgUrl = d.ImgUrl
            }).ToListAsync(ct);
        }

        private static RenderType GetRenderType(string? type) =>
            (type ?? "pdf").ToLowerInvariant() switch
            {
                "pdf" => RenderType.Pdf,
                "excel" => RenderType.Excel,
                "word" => RenderType.Word,
                "html" => RenderType.Html,
                _ => throw new ArgumentException("Invalid report type. Use: pdf, excel, word, html")
            };
    }

    // Optional filter DTO for your endpoint
    public sealed class ReportFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Subject { get; set; }
        public string? Editor { get; set; }
        public List<int>? Ids { get; set; }
        public int? Take { get; set; } = 100;
        public string? Title { get; set; }
    }

}
