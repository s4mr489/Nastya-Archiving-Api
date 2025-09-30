using AutoMapper;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
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
using Nastya_Archiving_project.Models.DTOs.Reports;
using Nastya_Archiving_project.Models.Entity;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.files;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text;
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
        /// Private helper method to log user actions in the UsersEditing table
        /// </summary>
        /// <param name="model">Name of the module/model being modified</param>
        /// <param name="tableName">Name of the table being modified</param>
        /// <param name="tableNameA">Alternative name of the table if applicable</param>
        /// <param name="recordId">ID of the record being modified</param>
        /// <param name="recordData">JSON or string representation of the record data</param>
        /// <param name="operationType">Type of operation (e.g., "ADD", "EDIT", "DELETE")</param>
        /// <param name="accountUnitId">Account unit ID</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task LogUserAction(
            string model,
            string tableName,
            string tableNameA,
            string recordId,
            string recordData,
            string operationType,
            int? accountUnitId)
        {
            try
            {
                // Create the log entry with the correct property names matching the database columns
                var logEntry = new UsersEditing
                {
                    Model = model,
                    TblName = tableName,
                    TblNameA = tableNameA,
                    RecordId = recordId, // Note: In code it's RecordId but DB column is RecordID
                    RecordData = recordData,
                    OperationType = operationType,
                    AccountUnitId = accountUnitId,
                    Editor = (await _systemInfoServices.GetRealName()).RealName,
                    EditDate = DateTime.UtcNow,
                    Ipadress = await _systemInfoServices.GetUserIpAddress() // Note: In code it's Ipadress but DB column is IPAdress
                };

                // Add the entity to the DbSet
                _context.UsersEditings.Add(logEntry);
                
                // Save changes with current context
                await _context.SaveChangesAsync();
                
                // Log to console for debugging
                Console.WriteLine($"Log saved: {operationType} on {tableName}, Record ID: {recordId}");
            }
            catch (Exception ex)
            {
                // Log the exception details for troubleshooting
                Console.WriteLine($"Error logging user action: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Helper method to format record data as key-value pairs separated by # characters
        /// </summary>
        /// <param name="doc">ArcivingDoc entity to format</param>
        /// <returns>Formatted record data string</returns>
        private string FormatRecordData(ArcivingDoc doc)
        {
            var sb = new StringBuilder();
            
            sb.Append($"id={doc.Id}");
            sb.Append($"#RefrenceNo={doc.RefrenceNo}");
            sb.Append($"#DocID={doc.DocId}");
            sb.Append($"#DocNo={doc.DocNo}");
            sb.Append($"#DocDate={doc.DocDate?.ToString("M/d/yyyy")}");
            sb.Append($"#DocSource={doc.DocSource}");
            sb.Append($"#DocTarget={doc.DocTarget}");
            sb.Append($"#Subject={doc.Subject}");
            sb.Append($"#WordsTosearch={doc.WordsTosearch}");
            sb.Append($"#ImgURL={doc.ImgUrl}");
            sb.Append($"#DocTitle={doc.DocTitle}");
            sb.Append($"#BoxfileNo={doc.BoxfileNo}");
            sb.Append($"#DocType={doc.DocType}");
            sb.Append($"#DepartID={doc.DepartId}");
            sb.Append($"#Editor={doc.Editor}");
            sb.Append($"#EditDate={doc.EditDate?.ToString("M/d/yyyy h:mm:ss tt")}");
            sb.Append($"#AccountUnitID={doc.AccountUnitId}");
            sb.Append($"#theyear={doc.Theyear}");
            sb.Append($"#TheWay={doc.TheWay}");
            sb.Append($"#SystemID={doc.SystemId}");
            sb.Append($"#sequre={doc.Sequre}");
            sb.Append($"#DocSize={doc.DocSize?.ToString("0.00")}");
            sb.Append($"#BranchID={doc.BranchId}");
            sb.Append($"#Notes={doc.Notes}");
            sb.Append($"#FileType={doc.FileType}");
            sb.Append($"#TheMonth={doc.TheMonth}");
            sb.Append($"#SubDocType={doc.SubDocType}");
            sb.Append($"#fourth={doc.Fourth}");
            sb.Append($"#IPAddress={doc.Ipaddress}");
            sb.Append($"#HaseBakuped={doc.HaseBakuped}");
            sb.Append($"#ReferenceTo={doc.ReferenceTo}");
            
            return sb.ToString();
        }

        public async Task<(ArchivingDocsResponseDTOs? docs, string? error)> PostArchivingDocs(ArchivingDocsViewForm req, FileViewForm file)
        {
            //check the docs if exists By docs Number and docs type Id
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(e => e.DocNo == req.DocNo && e.DocType == req.DocType);
            if (docs != null)
                return (null, "This document already exists.");

            // Inside your method, for example in PostArchivingDocs:
            var claimsIdentity = (ClaimsIdentity)_httpContext.HttpContext.User.Identity;

            string? branchId = claimsIdentity.FindFirst("BranchId")?.Value;
            string? departId = claimsIdentity.FindFirst("DepartId")?.Value;
            string? accountUnitId = claimsIdentity.FindFirst("AccountUnitId")?.Value;
            string? fileType = claimsIdentity.FindFirst("FileType")?.Value;

            var userId = await _systemInfoServices.GetUserId();
            if (userId.Id == null)
                return (null, "403"); // Unauthorized

            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(u => u.UserId.ToString() == userId.Id);

            if (!int.TryParse(userId.Id, out int userIdInt))
                return (null, "Invalid user ID.");
            var hasPermission = await _systemInfoServices.CheckUserHaveDepart(req.DepartId, userIdInt);

            if (userPermissions.AllowAddToOther == 0 && hasPermission == false && req.DepartId.ToString() != departId ||
               userPermissions.AllowAddToOther == 1 && hasPermission == false && req.DepartId.ToString() != departId)
                return (null, "403"); // Forbidden

            var docTypeResponse = await _archivingSettingsServicers.GetDocsTypeById(req.DocType);
            if (docTypeResponse.docsType == null)
                return (null, "Invalid document type.");

            // Set document type description for file upload
            file.DocTypeDscrption = docTypeResponse.docsType.docuName;

            // Upload file only once and store the result
            var uploadResult = await _fileServices.upload(file);
            if (uploadResult.error != null)
                return (null, $"File upload failed: {uploadResult.error}");

            // If the claim is not found, you can set it to null or handle it as needed
            // that manual mapper to the ArcivingDoc model
            var newDoc = new ArcivingDoc
            {
                RefrenceNo = await _systemInfoServices.GetLastRefNo(),
                DocNo = req.DocNo,
                DocDate = req.DocDate,
                DocSize = uploadResult.fileSize, // Use the file size from the upload result
                DocSource = req.DocSource,
                DocTarget = req.DocTarget,
                DocTitle = req.DocTitle,
                DocType = docTypeResponse.docsType.Id,
                SubDocType = req.SubDocType,
                DepartId = req.DepartId,
                BranchId = branchId != null ? int.Parse(branchId) : null,
                AccountUnitId = accountUnitId != null ? int.Parse(accountUnitId) : null,
                BoxfileNo = req.BoxfileNo,
                EditDate = DateTime.UtcNow,
                Editor = (await _systemInfoServices.GetRealName()).RealName,
                Ipaddress = (await _systemInfoServices.GetUserIpAddress()),
                ImgUrl = uploadResult.file, // Use the file path from the upload result
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

            // Format record data as key-value pairs separated by # characters
            string recordData = FormatRecordData(newDoc);

            // Log the document creation action
            await LogUserAction(
                model: "Archiving",
                tableName: "Arciving_Docs",
                tableNameA: "جدول ارشفة الوثائق",
                recordId: newDoc.RefrenceNo,
                recordData: recordData,
                operationType: "Add",
                accountUnitId: newDoc.AccountUnitId
            );

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

            // Store original values for logging
            var originalDocData = FormatRecordData(docs);

            // Keep track of whether the document type is changing
            bool isDocTypeChanging = req.DocType != 0 && req.DocType != docs.DocType;
            int originalDocType = docs.DocType;

            // Update properties
            docs.DocNo = req.DocNo ?? docs.DocNo;
            docs.DocDate = req.DocDate ?? docs.DocDate;
            docs.DocTitle = req.DocTitle ?? docs.DocTitle;
            docs.SubDocType = req.SubDocType != null ? req.SubDocType : null;
            docs.BoxfileNo = req.BoxfileNo ?? docs.BoxfileNo;
            docs.EditDate = DateTime.UtcNow;
            docs.Editor = (await _systemInfoServices.GetRealName()).RealName;
            docs.Ipaddress = (await _systemInfoServices.GetUserIpAddress());
            docs.Subject = req.Subject ?? docs.Subject;
            docs.ReferenceTo = req.ReferenceTo ?? docs.ReferenceTo;
            docs.Notes = req.Notes ?? docs.Notes;
            docs.WordsTosearch = req.WordsTosearch ?? docs.WordsTosearch;

            // If document type is changing, update it first
            if (isDocTypeChanging)
            {
                Console.WriteLine($"Document type changing from {originalDocType} to {req.DocType}");
                docs.DocType = docTypeResponse.docsType.Id;
            }

            // Only attempt to update file path if there's a valid ImgUrl and doc type is changing
            if (isDocTypeChanging && !string.IsNullOrEmpty(docs.ImgUrl))
            {
                try
                {
                    Console.WriteLine($"Attempting to update file path for document {docs.Id}. Current path: {docs.ImgUrl}");

                    // Try with both docuName and departmentName since we're not sure which one is needed
                    var changePath = await _fileServices.UpdateFilePathAsync(docs.ImgUrl, docTypeResponse.docsType.docuName);

                    if (changePath.error != null)
                    {
                        Console.WriteLine($"Error updating file path with docuName: {changePath.error}");

                        // If docuName fails, try with departmentName as fallback
                        if (!string.IsNullOrEmpty(docTypeResponse.docsType.departmentName))
                        {
                            Console.WriteLine($"Trying with departmentName: {docTypeResponse.docsType.departmentName}");
                            changePath = await _fileServices.UpdateFilePathAsync(docs.ImgUrl, docTypeResponse.docsType.departmentName);
                        }
                    }

                    if (changePath.error != null)
                    {
                        Console.WriteLine($"Error updating file path: {changePath.error}");
                    }
                    else if (!string.IsNullOrEmpty(changePath.updatedFilePath))
                    {
                        Console.WriteLine($"File path updated successfully: {changePath.updatedFilePath}");
                        docs.ImgUrl = changePath.updatedFilePath;
                    }
                    else
                    {
                        Console.WriteLine("File path update returned empty result");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during file path update: {ex.Message}");
                }
            }

            // Save changes to the database
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"Document {docs.Id} saved with ImgUrl: {docs.ImgUrl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving document changes: {ex.Message}");
                return (null, $"Error saving document: {ex.Message}");
            }

            // Log the document update action with the formatted data
            string updatedDocData = FormatRecordData(docs);
            await LogUserAction(
                model: "Archiving",
                tableName: "Arciving_Docs",
                tableNameA: "جدول ارشفة الوثائق",
                recordId: docs.RefrenceNo.ToString(),
                recordData: updatedDocData,
                operationType: "Update",
                accountUnitId: docs.AccountUnitId
            );

            var response = new ArchivingDocsResponseDTOs()
            {
                Id = docs.Id,
                RefrenceNo = docs.RefrenceNo,
                DocId = docs.DocId,
                DocNo = docs.DocNo,
                DocDate = docs.DocDate,
                DocSource = docs.DocSource,
                DocTarget = docs.DocTarget,
                Subject = docs.Subject,
                WordsTosearch = docs.WordsTosearch,
                ImgUrl = docs.ImgUrl,
                DocTitle = docs.DocTitle,
                BoxfileNo = docs.BoxfileNo,
                DocType = docs.DocType,
                DepartId = docs.DepartId,
                Editor = docs.Editor,
                EditDate = docs.EditDate,
                AccountUnitId = docs.AccountUnitId,
                Theyear = docs.Theyear,
                TheWay = docs.TheWay,
                SystemId = docs.SystemId,
                Sequre = docs.Sequre,
                DocSize = docs.DocSize,
                BranchId = docs.BranchId,
                Notes = docs.Notes,
                FileType = fileType != null ? int.Parse(fileType) : null,
                TheMonth = docs.TheMonth,
                SubDocType = docs.SubDocType,
                Fourth = docs.Fourth,
            };
            return (response, null);
        }


        //delete the archiving docs by Id
        public async Task<string> DeleteArchivingDocs(int Id)
        {
            var userId = await _systemInfoServices.GetUserId();
            if (userId.Id == null)
                return ("403"); // Unauthorized

            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(u => u.UserId.ToString() == userId.Id);
            if (userPermissions.AllowDelete != 1)
                return ("403"); // Forbidden

            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(d => d.Id == Id);
            if (docs == null)
                return ("404"); // docs not found
                
            // Format the document data for logging before deletion
            string docData = FormatRecordData(docs);

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
            
            // Log the document deletion
            await LogUserAction(
                model: "Archiving", 
                tableName: "Arciving_Docs",
                tableNameA: "جدول ارشفة الوثائق", 
                recordId: docs.RefrenceNo,
                recordData: docData,
                operationType: "Delete",
                accountUnitId: docs.AccountUnitId
            );

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

            var JoinedDocs = await _context.JoinedDocs.FirstOrDefaultAsync(d => d.ChildRefrenceNo == systemId);
            if (JoinedDocs != null)
            {
                // If the document is joined, remove the join entry
                _context.JoinedDocs.Remove(JoinedDocs);
                await _context.SaveChangesAsync();
            }

            docs.ReferenceTo = null; // Unbind the document by setting ReferenceTo to null

            
            await _context.SaveChangesAsync();  
            var response = _mapper.Map<ArchivingDocsResponseDTOs>(docs);
            return (response, null);
        }

        /// <summary>
        /// Restores a document from the deleted documents archive back to the active documents collection
        /// </summary>
        /// <param name="Id">The ID of the deleted document to restore</param>
        /// <returns>Status code: "200" for success, "404" if document not found</returns>
        public async Task<string> RestoreDeletedDocuments(int Id)
        {
            var docs = await _context.ArcivingDocsDeleteds.FirstOrDefaultAsync(d => d.Id == Id);
            if (docs == null)
                return "404";

            var restoredDocs = new ArcivingDoc
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
                DocType = docs.DocType.HasValue ? docs.DocType.Value : default(int),
                Notes = docs.Notes,
                SubDocType = docs.SubDocType,
                RefrenceNo = docs.RefrenceNo,
                Subject = docs.Subject,
                SystemId = docs.SystemId,
            };

            _context.ArcivingDocs.Add(restoredDocs);
            _context.ArcivingDocsDeleteds.Remove(docs);
            await _context.SaveChangesAsync();
            
            // Log the document restoration
            string restoredDocData = FormatRecordData(restoredDocs);
            await LogUserAction(
                model: "Archiving", 
                tableName: "Arciving_Docs",
                tableNameA: "جدول ارشفة الوثائق", 
                recordId: restoredDocs.RefrenceNo,
                recordData: restoredDocData,
                operationType: "Restore",
                accountUnitId: restoredDocs.AccountUnitId
            );

            return "200";// restore Docs Successfully
        }

        public async Task<BaseResponseDTOs> JoinDocsFromArchive(JoinedDocsViewForm req)
        {
            var docs = await _context.ArcivingDocs.FirstOrDefaultAsync(d => d.RefrenceNo == req.parentReferenceId);
            if (docs == null)
                return new BaseResponseDTOs(null, 404, "not Found Parent Docs");

            var childDocs = await _context.ArcivingDocs.FirstOrDefaultAsync(d => d.RefrenceNo == req.childReferenceId);
            if (childDocs == null)
                return new BaseResponseDTOs(null, 404, "not Found Child Docs");

            var joineDocs = await _context.JoinedDocs.FirstOrDefaultAsync(d => d.ParentRefrenceNO == req.parentReferenceId && d.ChildRefrenceNo == req.childReferenceId);
            if (joineDocs != null)
                return new BaseResponseDTOs(null, 400, "The Docs Already joind");


            var joinedDocs = new T_JoinedDoc
            {
                BreafcaseNo = req.breafcaseNo,
                ChildRefrenceNo = req?.childReferenceId,
                ParentRefrenceNO = req.parentReferenceId,
                editDate = DateTime.UtcNow,
            };
           
            var response = new JoinedDocsResponseDTOs
            {
                Breifexplanation = joinedDocs.BreafcaseNo,
                ReferenceTo = joinedDocs.ParentRefrenceNO,
                RefrenceNo = joinedDocs.ChildRefrenceNo,
            };
            docs.ReferenceTo = "True";
            childDocs.ReferenceTo = joinedDocs.ParentRefrenceNO;

            _context.JoinedDocs.Add(joinedDocs);
            await _context.SaveChangesAsync();
            
            // Log the document joining action
            string joinData = $"ParentRefrenceNO={joinedDocs.ParentRefrenceNO}#ChildRefrenceNo={joinedDocs.ChildRefrenceNo}#BreafcaseNo={joinedDocs.BreafcaseNo}#editDate={joinedDocs.editDate?.ToString("M/d/yyyy h:mm:ss tt")}";
            await LogUserAction(
                model: "Archiving", 
                tableName: "T_JoinedDoc",
                tableNameA: "جدول الوثائق المربوطة", 
                recordId: joinedDocs.ChildRefrenceNo.ToString(),
                recordData: joinData,
                operationType: "Join",
                accountUnitId: docs.AccountUnitId
            );

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

        public async Task<BaseResponseDTOs> UnbindDoucAllDocsFromTheParent(string parentSystemId)
        {
            var docs = await _context.ArcivingDocs.Where(d => d.ReferenceTo == parentSystemId).ToListAsync();
            if (docs == null || docs.Count == 0)
                return new BaseResponseDTOs(null, 404, "No documents found linked to the specified parent.");
            var parentDoc = await _context.ArcivingDocs.FirstOrDefaultAsync(d => d.RefrenceNo == parentSystemId);
            var joinedDocs = await _context.JoinedDocs.Where(d => d.ParentRefrenceNO == parentSystemId).ToListAsync();  
            if (joinedDocs != null && joinedDocs.Count > 0)
            {
                foreach(var join in joinedDocs)
                {
                    _context.JoinedDocs.Remove(join); // Remove each join entry
                }
            }
            foreach (var doc in docs)
            {
                doc.ReferenceTo = null; // Unbind each document by setting ReferenceTo to null
            }
            parentDoc.ReferenceTo = null; // Unbind the parent document as well

            await _context.SaveChangesAsync();
            return new BaseResponseDTOs(null, 200, "All linked documents have been unbound from the parent successfully.");
        }
        
        /// <summary>
        /// Gets a document by its reference number with all related details
        /// </summary>
        /// <param name="referenceNo">The reference number of the document to retrieve</param>
        /// <returns>A response with document details or an error message</returns>
        public async Task<BaseResponseDTOs> GetDocumentByReferenceNo(string referenceNo)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(referenceNo))
                {
                    return new BaseResponseDTOs(null, 400, "Reference number cannot be empty.");
                }

                // Find the document by reference number
                var document = await _context.ArcivingDocs
                    .AsNoTracking()
                    .Select( d => d)
                    .FirstOrDefaultAsync(d => d.RefrenceNo == referenceNo);

                if (document == null)
                {
                    return new BaseResponseDTOs(null, 404, $"Document with reference number '{referenceNo}' not found.");
                }

                // Get related data
                var (docTypeObj, _) = document.DocType > 0
                    ? await _archivingSettingsServicers.GetDocsTypeById(document.DocType)
                    : (null, null);

                var (docTargetObj, _) = document.DocTarget.HasValue && document.DocTarget.Value > 0
                    ? await _infrastructureServices.GetPOrganizationById(document.DocTarget.Value)
                    : (null, null);

                var (docSourceObj, _) = document.DocSource.HasValue && document.DocSource.Value > 0
                    ? await _infrastructureServices.GetPOrganizationById(document.DocSource.Value)
                    : (null, null);

                var (supDocTypeObj, _) = document.SubDocType.HasValue && document.SubDocType.Value > 0
                    ? await _archivingSettingsServicers.GetSupDocsTypeById(document.SubDocType.Value)
                    : (null, null);

                var (departmentObj, _) = document.DepartId.HasValue && document.DepartId.Value > 0
                    ? await _infrastructureServices.GetDepartmentById(document.DepartId.Value)
                    : (null, null);

                // Check if this document is referenced by others or references others
                var childDocuments = await _context.JoinedDocs
                    .Where(j => j.ParentRefrenceNO == document.RefrenceNo)
                    .Select(j => j.ChildRefrenceNo)
                    .ToListAsync();

                var parentReference = await _context.JoinedDocs
                    .Where(j => j.ChildRefrenceNo == document.RefrenceNo)
                    .Select(j => j.ParentRefrenceNO)
                    .FirstOrDefaultAsync();

                // Prepare the response object with enriched data
                var enrichedDocument = new
                {
                    Document = new
                    {
                        document.Id,
                        document.RefrenceNo,
                        document.DocNo,
                        document.DocType,
                        document.Subject,
                        document.DocSize,
                        document.Editor,
                        DocDate = document.DocDate,
                        document.EditDate,
                        document.BoxfileNo,
                        document.Notes,
                        document.ReferenceTo,
                        document.ImgUrl,
                        document.DepartId,
                        DepartmentName = departmentObj?.DepartmentName,
                        DocSourceName = docSourceObj?.Dscrp,
                        DocTargetName = docTargetObj?.Dscrp,
                        DocTypeName = docTypeObj?.docuName,
                        SupDocTypeName = supDocTypeObj?.supDocuName,
                    },
                    References = new
                    {
                        ChildDocuments = childDocuments,
                        ParentReference = parentReference,
                        IsParent = !string.IsNullOrEmpty(document.ReferenceTo) && document.ReferenceTo == "True",
                        IsChild = !string.IsNullOrEmpty(document.ReferenceTo) && document.ReferenceTo != "True" 
                    }
                };

                // Format the response to match the GeneralReport structure
                var response = new
                {
                    Data = enrichedDocument,
                    TotalCount = 1,
                    PageNumber = 1,
                    PageSize = 1
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error retrieving document by reference number: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return new BaseResponseDTOs(null, 500, $"An error occurred while retrieving the document: {ex.Message}");
            }
        }
    }
}