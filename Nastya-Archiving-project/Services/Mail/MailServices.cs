using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.mail;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;

namespace Nastya_Archiving_project.Services.Mail
{
    public class MailServices : IMailServices
    {

        private readonly AppDbContext _context;
        private readonly IArchivingSettingsServicers _archivingSettingsServicers;
        private readonly IInfrastructureServices _infrastructureServices;
        private readonly ISystemInfoServices _systemInfoServices;
        private readonly IEncryptionServices _encryptionServices;
        public MailServices(AppDbContext context,
                            IArchivingSettingsServicers archivingSettingsServicers,
                            IInfrastructureServices infrastructureServices,
                            ISystemInfoServices systemInfoServices = null,
                            IEncryptionServices encryptionServices = null)
        {
            _context = context;
            _archivingSettingsServicers = archivingSettingsServicers;
            _infrastructureServices = infrastructureServices;
            _systemInfoServices = systemInfoServices;
            _encryptionServices = encryptionServices;
        }


        public async Task<BaseResponseDTOs> SendMail(MailViewForm req)
        {
            try
            {
                var (realName, error) = await _systemInfoServices.GetRealName();

                if (!string.IsNullOrEmpty(error))
                    return new BaseResponseDTOs(null, 400, error);

                var mail = new TFileTransferring
                {
                    RefrenceNo = req.ReferenceNo,
                    From = realName,
                    To = req.to,
                    Notes = req.Notes,
                    SendDate = DateTime.UtcNow,
                    Readed = 0, // Default to 0 if null
                };

                // Save to database, perform other operations
                _context.TFileTransferrings.Add(mail);
                await _context.SaveChangesAsync();

                return new BaseResponseDTOs(mail, 200);
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, ex.Message);
            }
        }

        public async Task<BaseResponseDTOs> GetAllMails()
        {
            
            {
                var (realName, error) = await _systemInfoServices.GetRealName();
                if (!string.IsNullOrEmpty(error))
                    return new BaseResponseDTOs(null, 404, "Why You Dont Have RealName");


                // Fetch all mails from the database
                var mails = await _context.TFileTransferrings
                    .Where(u => u.To == realName)
                    .OrderByDescending(u => u.SendDate)
                    .ToListAsync();

                return new BaseResponseDTOs(mails, 200);

                //    if (mails.Any())
                //    {
                //        // Get reference numbers from mails
                //        var referenceNumbers = mails.Select(d => d.RefrenceNo).ToList();

                //        // Find associated documents if needed
                //        var docs = await _context.ArcivingDocs
                //            .Where(d => referenceNumbers.Contains(d.RefrenceNo))
                //            .Select(d => new
                //            {
                //                RefrenceNo = d.RefrenceNo,
                //                DocNO = d.DocNo,
                //                DocDate = d.DocDate,
                //                supDocType = d.SubDocType,
                //                subjec = d.Subject,
                //                notes = d.Notes,
                //                source = d.DocSource,
                //                targe = d.DocTarget,
                //            })
                //            .ToListAsync();

                //        var response = new MailResponseDTOs
                //        {
                //            // Populate your MailResponseDTOs properties as needed

                //            docc = docs,
                //            // Add other properties as needed
                //        };

                //        return new BaseResponseDTOs(response, 200);
                //    }

                //    return new BaseResponseDTOs(new MailResponseDTOs { docc = new List<TFileTransferring>() }, 200, "No mails found");
                //}
                //catch (Exception ex)
                //{
                //    return new BaseResponseDTOs(null, 500, ex.Message);
                //}
            }
        }

        public Task<BaseResponseDTOs> GetMailList(MailResponseDTOs result)
        {
            throw new NotImplementedException();
        }
    }
}
