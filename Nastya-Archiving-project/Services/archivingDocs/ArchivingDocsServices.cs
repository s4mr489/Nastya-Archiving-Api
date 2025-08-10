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
using System.Text.Json;

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

        /// <summary>
        /// Logs user actions to the UsersEditing table
        /// </summary>
        /// <param name="tableName">The name of the table being edited</param>
        /// <param name="recordId">The ID of the record being edited</param>
        /// <param name="recordData">JSON representation of the record data</param>
        /// <param name="operationType">Type of operation (Create, Update, Delete)</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task LogUserAction(string tableName, string recordId, object recordData, string operationType)
        {
            try
            {
                if (_httpContext?.HttpContext?.User?.Identity == null)
                {
                    Console.WriteLine("Warning: Cannot log user action - HttpContext or User Identity is null");
                    return;
                }

                var claimsIdentity = (ClaimsIdentity)_httpContext.HttpContext.User.Identity;
                string? accountUnitId = claimsIdentity.FindFirst("AccountUnitId")?.Value;
                
                // Serialize the data with options to limit size if needed
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    MaxDepth = 10
                };

                // Limit the size of the data to fit in the database field (1024 chars max)
                string serializedData = JsonSerializer.Serialize(recordData, jsonOptions);
                if (serializedData.Length > 1000) // Leave some buffer space
                {
                    // Truncate and add an indicator
                    serializedData = serializedData.Substring(0, 990) + "...[truncated]";
                }

                // Get user info first to avoid db context conflicts
                string realName = (await _systemInfoServices.GetRealName()).RealName;
                string ipAddress = await _systemInfoServices.GetUserIpAddress();

                // Create log entry
                var userLog = new UsersEditing
                {
                    Model = "ArcivingDoc", // The model name
                    TblName = tableName, // Table name in English
                    TblNameA = tableName, // Table name in Arabic (using same value for now)
                    RecordId = recordId,
                    RecordData = serializedData,
                    OperationType = operationType,
                    AccountUnitId = accountUnitId != null ? int.Parse(accountUnitId) : null,
                    Editor = realName,
                    EditDate = DateTime.UtcNow,
                    Ipadress = ipAddress
                };

                // Use a separate context instance to avoid conflicts with the main operation's context
                using (var logContext = new AppDbContext(
                    _context.Database.GetDbConnection().CreateNewConnectionScope() as DbContextOptions<AppDbContext>, 
                    (_context as AppDbContext).GetConfiguration()))
                {
                    logContext.UsersEditings.Add(userLog);
                    await logContext.SaveChangesAsync();
                }
                
                Console.WriteLine($"User action logged successfully: {operationType} on {tableName} record {recordId}");
            }
            catch (Exception ex)
            {
                // Log the exception but don't interrupt the main flow
                Console.WriteLine($"Error logging user action: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        public async Task<(ArchivingDocsResponseDTOs? docs, string? error)> PostArchivingDocs(ArchivingDocsViewForm req, FileViewForm file)
        {
            //check the docs if exists By docs  Number and docs type Id
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(e => e.DocNo == req.DocNo && e.DocType == req.DocType);
            if (docs != null)
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
                ImgUrl = (await _fileServices.upload(file)).file,
                FileType = fileType != null ? int.Parse(fileType) : null,
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

            // Log the creation action after the main operation is successfully completed
            await LogUserAction("ArcivingDocs", newDoc.Id.ToString(), new
            {
                Action = "Create",
                DocNo = newDoc.DocNo,
                DocType = newDoc.DocType,
                DocTitle = newDoc.DocTitle,
                RefrenceNo = newDoc.RefrenceNo
            }, "Create");

            var response = _mapper.Map<ArchivingDocsResponseDTOs>(newDoc);
            return (response, null);
        }

        //edit the arhciving docs by Id 
        public async Task<(ArchivingDocsResponseDTOs? docs, string? error)> EditArchivingDocs(ArchivingDocsViewForm req, int Id)
        {
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(e => e.Id == Id);
            if (docs == null)
                return (null, "Document not found.");

            // Store original data for logging
            var originalDocSummary = new
            {
                DocNo = docs.DocNo,
                DocDate = docs.DocDate,
                DocTitle = docs.DocTitle,
                DocType = docs.DocType,
                SubDocType = docs.SubDocType,
                Subject = docs.Subject
            };

            var claimsIdentity = (ClaimsIdentity)_httpContext.HttpContext.User.Identity;
            string? branchId = claimsIdentity.FindFirst("BranchId")?.Value;
            string? departId = claimsIdentity.FindFirst("DepartId")?.Value;
            string? accountUnitId = claimsIdentity.FindFirst("AccountUnitId")?.Value;
            string? fileType = claimsIdentity.FindFirst("FileType")?.Value;

            var docTypeResponse = await _archivingSettingsServicers.GetDocsTypeById(req.DocType);
            if (docTypeResponse.docsType == null)
                return (null, "Invalid document type.");
            var SupDocTypeResponse = await _archivingSettingsServicers.GetSupDocsTypeById(req.SubDocType);
            if (SupDocTypeResponse.supDocsType == null)
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

            // Log the update action with both original and updated data
            var updatedDocSummary = new
            {
                DocNo = docs.DocNo,
                DocDate = docs.DocDate,
                DocTitle = docs.DocTitle,
                DocType = docs.DocType,
                SubDocType = docs.SubDocType,
                Subject = docs.Subject
            };

            var logData = new 
            {
                Action = "Update",
                Original = originalDocSummary,
                Updated = updatedDocSummary
            };
            
            await LogUserAction("ArcivingDocs", docs.Id.ToString(), logData, "Update");

            var response = _mapper.Map<ArchivingDocsResponseDTOs>(docs);
            return (response, null);
        }

        //delete the archiving docs by Id
        public async Task<string> DeleteArchivingDocs(int Id)
        {
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(d => d.Id == Id);
            if (docs == null)
                return ("404"); // docs not found

            // Create a summary of the document for logging
            var docSummary = new
            {
                Action = "Delete",
                DocNo = docs.DocNo,
                DocTitle = docs.DocTitle,
                DocType = docs.DocType,
                RefrenceNo = docs.RefrenceNo
            };

            // Log the deletion action
            await LogUserAction("ArcivingDocs", Id.ToString(), docSummary, "Delete");

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

        // that implementation used to remove the joined document by RefernceNo from the joinedDocs Entity and assigment the refernce to filed null
        public async Task<(ArchivingDocsResponseDTOs? docs, string? error)> UnbindDoucFromTheArchive(string systemId)
        {
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(d => d.RefrenceNo == systemId);
            if (docs == null)
                return (null, "Document not found.");

            // Create a summary of the document for logging
            var docSummary = new
            {
                Action = "Unbind",
                DocNo = docs.DocNo,
                DocTitle = docs.DocTitle,
                RefrenceNo = docs.RefrenceNo
            };

            // Log the unbind action
            await LogUserAction("ArcivingDocs", docs.Id.ToString(), docSummary, "Unbind");

            var JoinedDocs = await _context.JoinedDocs.FirstOrDefaultAsync(d => d.ChildRefrenceNo == systemId);
            if (JoinedDocs != null)
            {
                // If the document is joined, remove the join entry
                _context.JoinedDocs.Remove(JoinedDocs);
                await _context.SaveChangesAsync();
            }

            docs.ReferenceTo = null; // Unbind the document by setting ReferenceTo to null

            // Remove the document from the archive
            _context.ArcivingDocs.Remove(docs);
            await _context.SaveChangesAsync();
            var response = _mapper.Map<ArchivingDocsResponseDTOs>(docs);
            return (response, null);
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
                DocType = docs.DocType.HasValue ? docs.DocType.Value : default(int),// <-- FIXED LINE,
                Notes = docs.Notes,
                SubDocType = docs.SubDocType,
                RefrenceNo = docs.RefrenceNo,
                Subject = docs.Subject,
                SystemId = docs.SystemId,
            };

            _context.ArcivingDocs.Add(resotredDocs);
            _context.ArcivingDocsDeleteds.Remove(docs);
            await _context.SaveChangesAsync();

            // Create a summary of the document for logging
            var docSummary = new
            {
                Action = "Restore",
                DocNo = resotredDocs.DocNo,
                DocTitle = resotredDocs.DocTitle,
                RefrenceNo = resotredDocs.RefrenceNo
            };

            // Log the restore action
            await LogUserAction("ArcivingDocs", resotredDocs.Id.ToString(), docSummary, "Restore");

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
                editDate = DateTime.UtcNow,
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

            // Create a summary of the join operation for logging
            var joinSummary = new
            {
                Action = "Join",
                ParentReference = joinedDocs.ParentRefrenceNO,
                ChildReference = joinedDocs.ChildRefrenceNo,
                BriefcaseNo = joinedDocs.BreafcaseNo
            };

            // Log the join action
            await LogUserAction("JoinedDocs", joinedDocs.ChildRefrenceNo ?? "", joinSummary, "Join");

            return new BaseResponseDTOs(response, 200, "Docs Joined Successfully");
        }

        /// <summary>
        /// Gets image URLs from the ArcivingDocs entity with pagination support
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="getLastImage">If true, returns only the last image URL</param>
        /// <param name="getFirstImage">If true, returns only the first image URL</param>
        /// <returns>A tuple with list of image URLs and error message if any</returns>
        public async Task<(List<ImageUrlDTO>? imageUrls, string? error, int totalCount)> GetArchivingDocImages(
            int page = 1,
            int pageSize = 10,
            bool getLastImage = false,
            bool getFirstImage = false)
        {
            try
            {
                // Basic validation
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // Limit maximum page size

                // Base query to get documents with image URLs that are not null or empty
                var query = _context.ArcivingDocs
                    .Where(d => !string.IsNullOrEmpty(d.ImgUrl))
                    .OrderBy(d => d.Id);

                // Get total count for pagination metadata
                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                {
                    return (new List<ImageUrlDTO>(), "No images found", 0);
                }

                // Handle special cases - get only first or last image
                if (getLastImage)
                {
                    var lastDoc = await _context.ArcivingDocs
                        .Where(d => !string.IsNullOrEmpty(d.ImgUrl))
                        .OrderByDescending(d => d.Id)
                        .FirstOrDefaultAsync();

                    if (lastDoc != null)
                    {
                        var result = new List<ImageUrlDTO>
                        {
                            new ImageUrlDTO
                            {
                                Id = lastDoc.Id,
                                ImageUrl = lastDoc.ImgUrl,
                                DocNo = lastDoc.DocNo,
                                DocTitle = lastDoc.DocTitle,
                                ReferenceNo = lastDoc.RefrenceNo
                            }
                        };
                        return (result, null, totalCount);
                    }
                    else
                    {
                        return (new List<ImageUrlDTO>(), "No images found", 0);
                    }
                }

                if (getFirstImage)
                {
                    var firstDoc = await _context.ArcivingDocs
                        .Where(d => !string.IsNullOrEmpty(d.ImgUrl))
                        .OrderBy(d => d.Id)
                        .FirstOrDefaultAsync();

                    if (firstDoc != null)
                    {
                        var result = new List<ImageUrlDTO>
                        {
                            new ImageUrlDTO
                            {
                                Id = firstDoc.Id,
                                ImageUrl = firstDoc.ImgUrl,
                                DocNo = firstDoc.DocNo,
                                DocTitle = firstDoc.DocTitle,
                                ReferenceNo = firstDoc.RefrenceNo
                            }
                        };
                        return (result, null, totalCount);
                    }
                    else
                    {
                        return (new List<ImageUrlDTO>(), "No images found", 0);
                    }
                }

                // Calculate pagination
                var itemsToSkip = (page - 1) * pageSize;

                // Get paginated results
                var paginatedDocs = await query
                    .Skip(itemsToSkip)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTOs
                var imageUrls = paginatedDocs.Select(doc => new ImageUrlDTO
                {
                    Id = doc.Id,
                    ImageUrl = doc.ImgUrl,
                    DocNo = doc.DocNo,
                    DocTitle = doc.DocTitle,
                    ReferenceNo = doc.RefrenceNo
                }).ToList();

                return (imageUrls, null, totalCount);
            }
            catch (Exception ex)
            {
                return (null, $"An error occurred while retrieving image URLs: {ex.Message}", 0);
            }
        }
    }
}
