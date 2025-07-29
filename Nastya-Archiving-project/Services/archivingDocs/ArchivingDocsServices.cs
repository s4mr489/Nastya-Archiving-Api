using AutoMapper;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Identity.Client;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.JoinedDocs;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs.Linkdocuments;
using Nastya_Archiving_project.Models.DTOs.file;
using Nastya_Archiving_project.Models.Entity;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.files;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;
using System.Runtime.Serialization;
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


        //edit the arhciving docs by Id 
        public async Task<(ArchivingDocsResponseDTOs? docs, string? error)> EditArchivingDocs(ArchivingDocsViewForm req, int Id)
        {
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(e => e.Id == Id);
            if (docs == null)
                return (null, "Document not found.");

            var claimsIdentity = (ClaimsIdentity)_httpContext.HttpContext.User.Identity;
            string? branchId = claimsIdentity.FindFirst("BranchId")?.Value;
            string? departId = claimsIdentity.FindFirst("DepartId")?.Value;
            string? accountUnitId = claimsIdentity.FindFirst("AccountUnitId")?.Value;
            string? fileType = claimsIdentity.FindFirst("FileType")?.Value;

            var docTypeResponse = await _archivingSettingsServicers.GetDocsTypeById(req.DocType);
            if (docTypeResponse.docsType == null)
                return (null, "Invalid document type.");
            var SupDocTypeResponse = await _archivingSettingsServicers.GetSupDocsTypeById(req.SubDocType);
            if(SupDocTypeResponse.supDocsType == null)
                return (null, "Invalid sub-document type.");
            // Update properties
            docs.DocNo = req.DocNo;
            docs.DocDate = req.DocDate;
            docs.DocSource = req.DocSource;
            docs.DocTarget = req.DocTarget;
            docs.DocTitle = req.DocTitle;
            docs.DocType = docTypeResponse.docsType.Id;
            docs.SubDocType = SupDocTypeResponse.supDocsType.Id;
            docs.DepartId = departId != null ? int.Parse(departId) : null;
            docs.BranchId = branchId != null ? int.Parse(branchId) : null;
            docs.AccountUnitId = accountUnitId != null ? int.Parse(accountUnitId) : null;
            docs.BoxfileNo = req.BoxfileNo;
            docs.EditDate = DateTime.UtcNow;
            docs.Editor = (await _systemInfoServices.GetRealName()).RealName;
            docs.Ipaddress = (await _systemInfoServices.GetUserIpAddress());
            docs.Subject = req.Subject;
            docs.ReferenceTo = req.ReferenceTo;
            docs.Notes = req.Notes;
            docs.WordsTosearch = req.WordsTosearch;

            await _context.SaveChangesAsync();

            var response = _mapper.Map<ArchivingDocsResponseDTOs>(docs);
            return (response, null);
        }


        //delete the archiving docs by Id
        public async Task<string> DeleteArchivingDocs(int Id)
        {
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(d => d.Id == Id);
            if (docs == null)
                return ("404"); // docs not found

            var deletedDcos = new ArcivingDocsDeleted
            {
                Id = docs.Id,
                AccountUnitId = docs.AccountUnitId,
                BoxfileNo = docs.BoxfileNo,
                BranchId = docs.BranchId,
                DepartId = docs.DepartId,
                Sequre = docs.Sequre,
                DocDate = docs.DocDate.HasValue ? docs.DocDate.Value.ToDateTime(TimeOnly.MinValue) : null, // <-- FIXED LINE
                EditDate = docs.EditDate,
                DocTarget = docs.DocTarget,
                FileType = docs.FileType,
                Editor = (await _systemInfoServices.GetRealName()).RealName,
                Fourth = docs.Fourth,
                ImgUrl = docs.ImgUrl,
                HaseBakuped = 0,
                Ipaddress = docs.Ipaddress,
                DocSize = docs.DocSize,
                TheMonth = docs.TheMonth,
                TheWay = docs.TheWay,
                Theyear = docs.Theyear,
                DocId = docs.DocId,
                DocNo = docs.DocNo,
                DocSource = docs.DocSource,
                WordsTosearch = docs.WordsTosearch,
                DocTitle = docs.DocTitle,
                DocType = docs.DocType,// <-- FIXED LINE,
                Notes = docs.Notes,
                SubDocType = docs.SubDocType,
                RefrenceNo = docs.RefrenceNo,
                Subject = docs.Subject,
                SystemId = docs.SystemId,

            };

            _context.ArcivingDocsDeleteds.Add(deletedDcos);
            _context.ArcivingDocs.Remove(docs);
            await _context.SaveChangesAsync();

            return ("200");//remove successfully
        }


        // that implementation used to get the next reference number
        public async Task<string> GetNextRefernceNo()
        {
            var refe = await _systemInfoServices.GetLastRefNo();
            return (refe);
        }


        //not implemented yet
        public Task<(LinkdocumentsResponseDTOs? docs, string? error)> Linkdocuments(LinkdocumentsViewForm req, int Id)
        {
            throw new NotImplementedException();
        }

        // that implementation used to get all the archiving docs
        public Task<(List<ArchivingDocsResponseDTOs>? docs, string? error)> GetAllArchivingDocs()
        {
            throw new NotImplementedException();
        }


        // that implementation used to remove the document by Id
        public async Task<(ArchivingDocsResponseDTOs? docs, string? error)> GetArchivingDocsById(int Id)
        {
            throw new NotImplementedException();
        }

        // that implementation used to Restore the docs from the archive
        public async Task<string> RestoreDeletedDocuments(int Id)
        {
            var docs = await _context.ArcivingDocsDeleteds.FirstOrDefaultAsync(d => d.Id == Id);
            if (docs == null)
                return "404";

            var resotredDocs = new ArcivingDoc
            {
                AccountUnitId = docs.AccountUnitId,
                BoxfileNo = docs.BoxfileNo,
                BranchId = docs.BranchId, 
                DepartId = docs.DepartId,
                Sequre = docs.Sequre,
                DocDate = docs.DocDate.HasValue ? DateOnly.FromDateTime(docs.DocDate.Value) : null,
                EditDate = docs.EditDate,
                DocTarget = docs.DocTarget,
                FileType = docs.FileType,
                Editor = (await _systemInfoServices.GetRealName()).RealName,
                Fourth = docs.Fourth,
                ImgUrl = docs.ImgUrl,
                HaseBakuped = 0,
                Ipaddress = docs.Ipaddress ,
                DocSize = docs.DocSize,
                TheMonth = docs.TheMonth,
                TheWay = docs.TheWay,
                Theyear = docs.Theyear,
                DocId = docs.DocId,
                DocNo = docs.DocNo,
                DocSource = docs.DocSource,
                WordsTosearch = docs.WordsTosearch,
                DocTitle = docs.DocTitle,
                DocType = docs.DocType.HasValue ? docs.DocType.Value : default(int) ,// <-- FIXED LINE,
                Notes = docs.Notes,
                SubDocType = docs.SubDocType,
                RefrenceNo = docs.RefrenceNo,
                Subject = docs.Subject,
                SystemId = docs.SystemId,

            };

            _context.ArcivingDocs.Add(resotredDocs);
            _context.ArcivingDocsDeleteds.Remove(docs);
            await _context.SaveChangesAsync();

            return "200";// restore Docs Successfully

        }

        public async Task<BaseResponseDTOs> JoinDocsFromArchive(JoinedDocsViewForm req)
        {
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(d => d.RefrenceNo == req.parentReferenceId);
            if (docs == null)
                return new BaseResponseDTOs(null, 404, "not Found Parent Docs");

            var joineDocs = await _context.JoinedDocs.FirstOrDefaultAsync(d => d.ParentRefrenceNO == req.parentReferenceId && d.ChildRefrenceNo == req.childReferenceId);
            if (joineDocs != null)
                return new BaseResponseDTOs(null, 400, "The Docs Already joind");


            var joinedDocs = new T_JoinedDoc
            {
                BreafcaseNo = req.BreafcaseNo,
                ParentRefrenceNO = req.parentReferenceId,
                ChildRefrenceNo = req?.childReferenceId,
            };
           
            var response = new JoinedDocsResponseDTOs
            {
                Breifexplanation = joinedDocs.BreafcaseNo,
                ReferenceTo = joinedDocs.ParentRefrenceNO,
                RefrenceNo = joinedDocs.ChildRefrenceNo,
            };
            docs.ReferenceTo = joinedDocs.ChildRefrenceNo;

            _context.JoinedDocs.Add(joinedDocs);
            await _context.SaveChangesAsync();

            return new BaseResponseDTOs(response, 200, "Docs Joined Successfully");
        }
    }
}
