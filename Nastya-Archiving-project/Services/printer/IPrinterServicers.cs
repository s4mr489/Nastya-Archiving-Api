using Nastya_Archiving_project.Middleware;

namespace Nastya_Archiving_project.Services.printer
{
    public interface IPrinterServices
    {
        Task<bool> PrintDocument(string printerId, byte[] documentData, Dictionary<string, string> settings);
        List<PrinterInfo> GetAvailablePrinters();
    }
}
