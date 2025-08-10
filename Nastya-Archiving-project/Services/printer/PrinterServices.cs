using AutoMapper;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Middleware;

namespace Nastya_Archiving_project.Services.printer
{
    public class PrinterServices : BaseServices, IPrinterServices
    {
        public PrinterServices(IMapper mapper, AppDbContext context) : base(mapper, context)
        {
        }

        public async Task<bool> PrintDocument(string printerId, byte[] documentData, Dictionary<string, string> settings)
        {
            try
            {
                await PrinterWebSocketMiddleware.SendPrintJob(printerId, documentData, settings);
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error printing document: {ex.Message}");
                return false;
            }
        }

        public List<PrinterInfo> GetAvailablePrinters()
        {
            return PrinterWebSocketMiddleware.GetConnectedPrinters();
        }
    }
}
