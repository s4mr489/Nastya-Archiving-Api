namespace Nastya_Archiving_project.Services.rdlcReport
{
    public interface IRdlcReportServices
    {
        Task<byte[]> GenerateReportAsync(string reportName, string reportType, ReportFilter filter, CancellationToken ct = default);
    }
}
