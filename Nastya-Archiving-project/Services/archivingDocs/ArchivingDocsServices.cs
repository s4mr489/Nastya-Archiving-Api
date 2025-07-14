using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.files;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;
using System.Security.Claims;

namespace Nastya_Archiving_project.Services.archivingDocs
{
    public class ArchivingDocsServices : BaseServices, IArchivingDocsSercvices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISystemInfoServices _systemInfoServices;
        private readonly IInfrastructureServices _infrastructureServices;
        private readonly IArchivingSettingsServicers _archivingSettingsServicers;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IFilesServices _fileServices;
        public ArchivingDocsServices(AppDbContext context,
                                    IMapper mapper,
                                    ISystemInfoServices systemInfoServices,
                                    IInfrastructureServices infrastructureServices,
                                    IArchivingSettingsServicers archivingSettingsServicers,
                                    IHttpContextAccessor httpContext,
                                    IFilesServices fileServices) : base(mapper, context)
        {
            _context = context;
            _mapper = mapper;
            _systemInfoServices = systemInfoServices;
            _infrastructureServices = infrastructureServices;
            _archivingSettingsServicers = archivingSettingsServicers;
            _httpContext = httpContext;
            _fileServices = fileServices;
        }

        public Task<(List<ArchivingDocsResponseDTOs>? docs, string? error)> GetAllArchivingDocs()
        {
            throw new NotImplementedException();
        }

        public Task<(ArchivingDocsResponseDTOs? docs, string? error)> GetArchivingDocsById(int Id)
        {
            throw new NotImplementedException();
        }

        public async Task<(ArchivingDocsResponseDTOs? docs, string? error)> PostArchivingDocs(ArchivingDocsViewForm req ,FileViewForm file)
        {
            //check the docs if exists By docs  Number and docs type Id
            var docs =await  _context.ArcivingDocs.FirstOrDefaultAsync(e => e.DocNo== req.DocNo && e.DocType== req.DocType);
            if(docs != null)
                return (null, "This document already exists.");


            // Inside your method, for example in PostArchivingDocs:
            var claimsIdentity = (ClaimsIdentity)_httpContext.HttpContext.User.Identity;

            string? branchId = claimsIdentity.FindFirst("BranchId")?.Value;
            string? departId = claimsIdentity.FindFirst("DepartId")?.Value;
            string? accountUnitId = claimsIdentity.FindFirst("AccountUnitId")?.Value;
            string? fileType = claimsIdentity.FindFirst("FileType")?.Value;

            var docTypeResponse = await _archivingSettingsServicers.GetDocsTypeById(req.DocType);
            if (docTypeResponse.docsType == null)
                return (null, "Invalid document type.");


            // If the claim is not found, you can set it to null or handle it as needed
            // that manual mapper to the ArcivingDoc model
            var newDoc = new ArcivingDoc
            {
                RefrenceNo = await _systemInfoServices.GetLastRefNo(),
                DocNo = req.DocNo,
                DocDate = req.DocDate,
                DocSize = (await _fileServices.upload(file)).fileSize,
                DocSource = req.DocSource,
                DocTarget = req.DocTarget,
                DocTitle = req.DocTitle,
                DocType = docTypeResponse.docsType.Id,
                SubDocType = req.SubDocType,
                DepartId = departId != null ? int.Parse(departId) : null,
                BranchId = branchId != null ? int.Parse(branchId) : null,
                AccountUnitId = accountUnitId != null ? int.Parse(accountUnitId) : null,
                BoxfileNo = req.BoxfileNo,
                EditDate = DateTime.UtcNow,
                Editor = (await _systemInfoServices.GetRealName()).RealName,
                Ipaddress = (await _systemInfoServices.GetUserIpAddress()),
                ImgUrl =(await _fileServices.upload(file)).file,
                FileType  = fileType != null ? int.Parse(fileType) : null,
                Subject = req.Subject,
                TheMonth = DateTime.UtcNow.Month,
                Theyear = DateTime.UtcNow.Year,
                ReferenceTo = req.ReferenceTo,
                Notes = req.Notes,
                WordsTosearch = req.WordsTosearch,
                HaseBakuped = 0, // Assuming HaseBakuped is a flag indicating if the document has been backed up, defaulting to 0 (not backed up)
            };

            // Add the new document to the context
            _context.ArcivingDocs.Add(newDoc);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ArchivingDocsResponseDTOs>(newDoc);
            return (response, null);
        }

        public Task<(ArchivingDocsResponseDTOs? docs, string? error)> EditArchivingDocs(ArchivingDocsViewForm req, int Id)
        {
            throw new NotImplementedException();
        }
        public Task<string> DeleteArchivingDocs(int Id)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetNextRefernceNo()
        {
            var refe = await _systemInfoServices.GetLastRefNo();
            return (refe);
        }
    }
}
