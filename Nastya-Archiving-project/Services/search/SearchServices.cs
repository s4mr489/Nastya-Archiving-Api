using AutoMapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using FYP.Extentions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Extinstion;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search;
using Nastya_Archiving_project.Models.DTOs.Search.CasesSearch;
using Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch;
using Nastya_Archiving_project.Models.DTOs.Search.QuikSearch;
using Nastya_Archiving_project.Models.DTOs.Search.TreeSearch;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Models.Entity;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.SystemInfo;
using Org.BouncyCastle.Ocsp;

namespace Nastya_Archiving_project.Services.search
{
    public class SearchServices :  ISearchServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEncryptionServices _encryptionServices;
        private readonly ISystemInfoServices _systemInfoServices;
        public SearchServices(AppDbContext context, IMapper mapper, 
                                IEncryptionServices encryptionServices,    
                                ISystemInfoServices systemInfoServices)
        {
            _context = context;
            _mapper = mapper;
            _encryptionServices = encryptionServices;
            _systemInfoServices = systemInfoServices;
        }


        public async Task<(List<QuikSearchResponseDTOs>? docs, string? error)> QuikeSearch(QuikeSearchViewForm req)
        {

            // Get user ID
            var userIdResult = await _systemInfoServices.GetUserId();
            if (userIdResult.Id == null)
                return (null, "401"); // Unauthorized

            // Parse the user ID to integer
            if (!int.TryParse(userIdResult.Id, out int userIdInt))
                return (null, "400"); // Bad request - invalid user ID format

            // Get user details
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdInt);
            if (user == null)
                return (null, "404"); // User not found

            // Get user permissions
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(p => p.UserId == userIdInt);
            if (userPermissions == null)
                return (null, "403"); // No permissions found

            // Check department access permission
            if (userPermissions.AllowViewTheOther == 0 && req.departId.HasValue && req.departId != user.DepariId)
            {
                // If user doesn't have general permission to view other departments,
                // check if they have specific permission for the requested department
                bool hasAccess = await _systemInfoServices.CheckUserHaveDepart(req.departId ?? 0, userIdInt);
                if (!hasAccess)
                    return (null, "403"); // Forbidden - cannot view other departments
            }

            try
            {
                // Start with a query on ArcivingDocs
                var query = _context.ArcivingDocs.AsQueryable();

                // Build a filter object
                var filter = new BaseFilter
                {
                    StartDate = req.from,
                    EndDate = req.to,
                };

                // Apply base filtering
                query = query.WhereBaseFilter(filter);

                // Apply all the filters from the request
                if (!string.IsNullOrWhiteSpace(req.docsNumber))
                {
                    if (req.exactMatch)
                        query = query.Where(d => d.DocNo != null && d.DocNo == req.docsNumber);
                    else
                        query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));
                }
                if (!string.IsNullOrWhiteSpace(req.subject))
                {
                    if(req.exactMatch)
                        query = query.Where(d => d.Subject != null && d.Subject == req.subject);
                    else
                        query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));
                }
                if(!string.IsNullOrEmpty(req.editor))
                    query = query.Where(d => d.Editor != null && d.Editor.Contains(req.editor));
                if (!string.IsNullOrWhiteSpace(req.systemId))
                {
                    query = query.Where(d => d.RefrenceNo != null && d.RefrenceNo == req.systemId);
                }
                if (req.docsType.HasValue)
                {
                    query = query.Where(d => d.DocType == req.docsType.Value);
                }
                if (!string.IsNullOrWhiteSpace(req.searchIntelligence))
                {
                    var words = req.searchIntelligence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        query = query.Where(d => d.WordsTosearch != null && d.WordsTosearch.Contains(word));
                    }
                }
                if (!string.IsNullOrWhiteSpace(req.wordToSearch))
                    query = query.Where(d => d.Notes != null && d.Notes.Contains(req.wordToSearch));
                if (!string.IsNullOrWhiteSpace(req.boxFile))
                    query = query.Where(d => d.BoxfileNo != null && d.BoxfileNo.Contains(req.boxFile));
                if (req.source.HasValue)
                    query = query.Where(d => d.DocSource != null && d.DocSource.Value == req.source.Value);
                if (req.ReferenceTo.HasValue)
                    query = query.Where(d => d.ReferenceTo != null && d.ReferenceTo == req.ReferenceTo.Value.ToString());
                if (req.fileType.HasValue)
                    query = query.Where(d => d.FileType.HasValue && d.FileType.Value == (int)req.fileType.Value);
                if (req.departId.HasValue)
                    query = query.Where(d => d.DepartId.HasValue && d.DepartId.Value == req.departId.Value);

                // Date filters
                if (req.editDate == true)
                {
                    if (req.from.HasValue)
                        query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value >= req.from.Value);
                    if (req.to.HasValue)
                        query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value <= req.to.Value);
                }
                if (req.docsDate == true)
                {
                    if (req.from.HasValue)
                        query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value >= DateOnly.FromDateTime(req.from.Value));
                    if (req.to.HasValue)
                        query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value <= DateOnly.FromDateTime(req.to.Value));
                }

                // Apply paging
                int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
                int pageSize = req.pageSize > 0 ? req.pageSize : 20;

                // Get the filtered documents with join information in a single query
                var pagedResults = await (
                    from doc in query
                    orderby doc.Id descending
                    let hasJoinedDocs = _context.JoinedDocs.Any(j => j.ParentRefrenceNO == doc.RefrenceNo)
                    select new { Document = doc, HasJoinedDocs = hasJoinedDocs }
                )
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

                // If no documents found, return early
                if (pagedResults.Count == 0)
                    return (null, "No documents found matching the criteria.");

                // Get document types for names
                var docTypeIds = pagedResults.Select(r => r.Document.DocType).Distinct().ToList();
                var docTypes = await _context.ArcivDocDscrps
                    .Where(dt => docTypeIds.Contains(dt.Id))
                    .ToDictionaryAsync(dt => dt.Id, dt => dt.Dscrp);

                // Build the final response
                var result = pagedResults.Select(r => new QuikSearchResponseDTOs
                {
                    systemId = r.Document.RefrenceNo,
                    Id = r.Document.Id,
                    file = r.Document.ImgUrl,
                    editDate = r.Document.EditDate,
                    docsNumber = r.Document.DocNo,
                    docsDate = r.Document.DocDate,
                    subject = r.Document.Subject,
                    source = r.Document.DocSource?.ToString(),
                    ReferenceTo = r.Document.ReferenceTo,
                    fileType = r.Document.FileType?.ToString(),
                    docsTitle = docTypes.TryGetValue(r.Document.DocType, out var typeName) ? typeName : null,
                    HasJoinedDocs = r.HasJoinedDocs
                }).ToList();

                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error during search: {ex.Message}");
            }
        }
        //public async Task<(List<QuikSearchResponseDTOs>? docs, string? error)> QuikeSearch(QuikeSearchViewForm req)
        //{
        //    try
        //    {
        //        var query = _context.ArcivingDocs.AsQueryable();

        //        // Build a filter object (extend BaseFilter if needed)
        //        var filter = new BaseFilter
        //        {
        //            StartDate = req.from,
        //            EndDate = req.to,
        //        };

        //        // Use extension methods for base filtering
        //        query = query.WhereBaseFilter(filter);

        //        // Custom filters for all QuikeSearchViewForm properties
        //        if (!string.IsNullOrWhiteSpace(req.docsNumber))
        //        {
        //            if (req.exactMatch)
        //                query = query.Where(d => d.DocNo != null && d.DocNo == req.docsNumber);
        //            else
        //                query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));
        //        }
        //        if (!string.IsNullOrWhiteSpace(req.subject))
        //        {
        //            query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));
        //        }
        //        if (!string.IsNullOrWhiteSpace(req.systemId))
        //        {
        //            query = query.Where(d => d.RefrenceNo != null && d.RefrenceNo == req.systemId);
        //        }
        //        if (req.docsType.HasValue)
        //        {
        //            query = query.Where(d => d.DocType == req.docsType.Value);
        //        }
        //        // ArcivingDoc does not have SupDocType, so skip this filter

        //        if (!string.IsNullOrWhiteSpace(req.wordToSearch))
        //            query = query.Where(d => d.WordsTosearch != null && d.WordsTosearch.Contains(req.wordToSearch));
        //        if (!string.IsNullOrWhiteSpace(req.boxFile))
        //            query = query.Where(d => d.BoxfileNo != null && d.BoxfileNo.Contains(req.boxFile));
        //        if (req.source.HasValue)
        //            query = query.Where(d => d.DocSource != null && d.DocSource.Value == req.source.Value);
        //        if (req.ReferenceTo.HasValue)
        //            query = query.Where(d => d.ReferenceTo != null && d.ReferenceTo == req.ReferenceTo.Value.ToString());
        //        if (req.fileType.HasValue)
        //            query = query.Where(d => d.FileType.HasValue && d.FileType.Value == (int)req.fileType.Value);
        //        if (req.departId.HasValue)
        //            query = query.Where(d => d.DepartId.HasValue && d.DepartId.Value == req.departId.Value);

        //        // Date filters for editDate and docsDate
        //        if (req.editDate == true)
        //        {
        //            if (req.from.HasValue)
        //                query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value >= req.from.Value);
        //            if (req.to.HasValue)
        //                query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value <= req.to.Value);
        //        }
        //        if (req.docsDate == true)
        //        {
        //            if (req.from.HasValue)
        //                query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value >= DateOnly.FromDateTime(req.from.Value));
        //            if (req.to.HasValue)
        //                query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value <= DateOnly.FromDateTime(req.to.Value));
        //        }

        //        // Paging and ordering
        //        int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
        //        int pageSize = req.pageSize > 0 ? req.pageSize : 20;

        //        var pagedDocs = await query
        //            .OrderByDescending(d => d.Id)
        //            .Skip((pageNumber - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToListAsync();

        //        // Get all doc types for mapping id to name
        //        var docTypeIds = pagedDocs.Select(d => d.DocType).Distinct().ToList();
        //        var docTypes = await _context.ArcivDocDscrps
        //            .Where(dt => docTypeIds.Contains(dt.Id))
        //            .ToListAsync();
        //        var docTypeNames = docTypes.ToDictionary(x => x.Id, x => x.Dscrp);

        //        var result = pagedDocs.Select(d => new QuikSearchResponseDTOs
        //        {
        //            systemId = d.RefrenceNo,
        //            Id = d.Id,
        //            file = d.ImgUrl,
        //            editDate = d.EditDate,
        //            docsNumber = d.DocNo,
        //            docsDate = d.DocDate,
        //            subject = d.Subject,
        //            source = d.DocSource != null ? d.DocSource.ToString() : null,
        //            ReferenceTo = d.ReferenceTo,
        //            fileType = d.FileType != null ? d.FileType.ToString() : null,
        //            // Add doctypeName to the response
        //            docsTitle = docTypeNames.ContainsKey(d.DocType) ? docTypeNames[d.DocType] : null
        //        }).ToList();

        //        if (result == null || result.Count == 0)
        //            return (null, "No documents found matching the criteria.");

        //        return (result, null);
        //    }
        //    catch (Exception ex)
        //    {
        //        return (null, $"Error during search: {ex.Message}");
        //    }
        //}

        /// <summary> not used year just for test </summary>
        public async Task<(List<QuikSearchResponseDTOs>? docs, string? error)> GetArcivingDocsAsync(
                string? docsNumber = null,
                string? subject = null,
                string? source = null,
                string? referenceTo = null,
                int? fileType = null,
                DateOnly? from = null,
                DateOnly? to = null,
                int pageNumber = 1,
                int pageSize = 20)
        {
            try
            {
                var sql = @"
            SELECT *
            FROM Arciving_Docs d
            WHERE 
                (@docsNumber IS NULL OR d.DocNo LIKE '%' + @docsNumber + '%')
                AND (@subject IS NULL OR d.Subject LIKE '%' + @subject + '%')
                AND (@source IS NULL OR CAST(d.DocSource AS NVARCHAR) LIKE '%' + @source + '%')
                AND (@referenceTo IS NULL OR d.ReferenceTo LIKE '%' + @referenceTo + '%')
                AND (@fileType IS NULL OR d.FileType = @fileType)
                AND (@from IS NULL OR d.DocDate >= @from)
                AND (@to IS NULL OR d.DocDate <= @to)
            ORDER BY d.Id DESC
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
        ";

                int skip = (pageNumber - 1) * pageSize;
                int take = pageSize;

                var docs = await _context.ArcivingDocs
                    .FromSqlRaw(sql,
                        new Microsoft.Data.SqlClient.SqlParameter("@docsNumber", (object?)docsNumber ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@subject", (object?)subject ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@source", (object?)source ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@referenceTo", (object?)referenceTo ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@fileType", (object?)fileType ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@from", (object?)from ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@to", (object?)to ?? DBNull.Value),
                        new Microsoft.Data.SqlClient.SqlParameter("@skip", skip),
                        new Microsoft.Data.SqlClient.SqlParameter("@take", take)
                    )
                    .ToListAsync();

                if (docs == null || docs.Count == 0)
                    return (null, "No documents found matching the criteria.");

                var result = docs.Select(d => new QuikSearchResponseDTOs
                {
                    systemId = d.RefrenceNo,
                    Id = d.Id,
                    file = d.ImgUrl,
                    docsNumber = d.DocNo,
                    docsDate = d.DocDate,
                    editDate = d.EditDate,
                    subject = d.Subject,
                    source = d.DocSource != null ? d.DocSource.ToString() : null,
                    ReferenceTo = d.ReferenceTo,
                    fileType = d.FileType != null ? d.FileType.ToString() : null
                }).ToList();


                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error during search: {ex.Message}");
            }
        }


        //<summary> This method is used to search for documents based on various criteria.
        public async Task<(List<DetialisSearchResponseDTOs>? docs, string? error)> DetailsSearch(QuikeSearchViewForm req)
        {
            // Get user ID
            var userIdResult = await _systemInfoServices.GetUserId();
            if (userIdResult.Id == null)
                return (null, "401"); // Unauthorized

            // Parse the user ID to integer
            if (!int.TryParse(userIdResult.Id, out int userIdInt))
                return (null, "400"); // Bad request - invalid user ID format

            // Get user details
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdInt);
            if (user == null)
                return (null, "404"); // User not found

            // Get user permissions
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(p => p.UserId == userIdInt);
            if (userPermissions == null)
                return (null, "403"); // No permissions found

            // Check department access permission
            if (userPermissions.AllowViewTheOther == 0 && req.departId.HasValue && req.departId != user.DepariId)
            {
                // If user doesn't have general permission to view other departments,
                // check if they have specific permission for the requested department
                bool hasAccess = await _systemInfoServices.CheckUserHaveDepart(req.departId ?? 0, userIdInt);
                if (!hasAccess)
                    return (null, "403"); // Forbidden - cannot view other departments
            }

            try
            {
                var query = _context.ArcivingDocs.AsQueryable();

                // Build a filter object (extend BaseFilter if needed)
                var filter = new BaseFilter
                {
                    StartDate = req.from,
                    EndDate = req.to,
                };

                // Use extension methods for base filtering
                query = query.WhereBaseFilter(filter);

                // Custom filters for all QuikeSearchViewForm properties
                if (!string.IsNullOrWhiteSpace(req.docsNumber))
                {
                    if (req.exactMatch)
                        query = query.Where(d => d.DocNo != null && d.DocNo == req.docsNumber);
                    else
                        query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));
                }
                if (!string.IsNullOrWhiteSpace(req.subject))
                {
                    query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));
                }
                if (!string.IsNullOrWhiteSpace(req.systemId))
                {
                    query = query.Where(d => d.RefrenceNo != null && d.RefrenceNo == req.systemId);
                }
                if (req.docsType.HasValue)
                {
                    query = query.Where(d => d.DocType != null && d.DocType == req.docsType);
                }
                // ArcivingDoc does not have SupDocType, so skip this filter


                if (!string.IsNullOrEmpty(req.editor))
                    query = query.Where(d => d.Editor != null && d.Editor.Contains(req.editor));

                if (!string.IsNullOrWhiteSpace(req.wordToSearch))
                    query = query.Where(d => d.Notes != null && d.Notes.Contains(req.wordToSearch));
                if (!string.IsNullOrWhiteSpace(req.boxFile))
                    query = query.Where(d => d.BoxfileNo != null && d.BoxfileNo.Contains(req.boxFile));
                if (req.source.HasValue)
                    query = query.Where(d => d.DocSource != null && d.DocSource.Value == req.source.Value);
                if (req.ReferenceTo.HasValue)
                    query = query.Where(d => d.ReferenceTo != null && d.ReferenceTo == req.ReferenceTo.Value.ToString());
                if (req.fileType.HasValue)
                    query = query.Where(d => d.FileType.HasValue && d.FileType.Value == (int)req.fileType.Value);
                if (req.departId.HasValue)
                    query = query.Where(d => d.DepartId.HasValue && d.DepartId.Value == req.departId.Value);
                if (!string.IsNullOrWhiteSpace(req.searchIntelligence))
                {
                    var words = req.searchIntelligence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        query = query.Where(d => d.WordsTosearch != null && d.WordsTosearch.Contains(word));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(req.wordToSearch))
                {
                    query = query.Where(d => d.Notes != null && d.Notes.Contains(req.wordToSearch));
                }

                // Date filters for editDate and docsDate
                if (req.editDate == true)
                {
                    if (req.from.HasValue)
                        query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value >= req.from.Value);
                    if (req.to.HasValue)
                        query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value <= req.to.Value);
                }
                if (req.docsDate == true)
                {
                    if (req.from.HasValue)
                        query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value >= DateOnly.FromDateTime(req.from.Value));
                    if (req.to.HasValue)
                        query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value <= DateOnly.FromDateTime(req.to.Value));
                }

                // Paging and ordering
                int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
                int pageSize = req.pageSize > 0 ? req.pageSize : 20;

                // Get the filtered documents with join information in a single query
                var pagedResults = await (
                    from doc in query
                    orderby doc.Id descending
                    let hasJoinedDocs = _context.JoinedDocs.Any(j => j.ParentRefrenceNO == doc.RefrenceNo)
                    select new { Document = doc, HasJoinedDocs = hasJoinedDocs }
                )
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

                // If no documents found, return early
                if (pagedResults.Count == 0)
                    return (null, "No documents found matching the criteria.");

                // Get document types for names
                var docTypeIds = pagedResults.Select(r => r.Document.DocType).Distinct().ToList();
                var docTypes = await _context.ArcivDocDscrps
                    .Where(dt => docTypeIds.Contains(dt.Id))
                    .ToListAsync();
                var docTypeNames = docTypes.ToDictionary(x => x.Id, x => x.Dscrp);

                // Get reference information from JoinedDocs table
                var refNos = pagedResults.Select(r => r.Document.RefrenceNo).ToList();
                var joinedDocs = await _context.JoinedDocs
                    .Where(j => refNos.Contains(j.ChildRefrenceNo))
                    .ToDictionaryAsync(j => j.ChildRefrenceNo, j => j.ParentRefrenceNO);

                var result = pagedResults.Select(r => new DetialisSearchResponseDTOs
                {
                    Id = r.Document.Id,
                    file = r.Document.ImgUrl,
                    docsNumber = r.Document.DocNo,
                    docsDate = r.Document.DocDate.HasValue ? r.Document.DocDate.Value : null,
                    editDate = r.Document.EditDate.HasValue ? r.Document.EditDate.Value : null,
                    subject = r.Document.Subject,
                    source = r.Document.DocSource != null ? r.Document.DocSource.ToString() : null,
                    ReferenceTo = joinedDocs.TryGetValue(r.Document.RefrenceNo, out var parentRef) ? parentRef : r.Document.ReferenceTo,
                    fileType = r.Document.FileType != null ? r.Document.FileType.ToString() : null,
                    supdocType = r.Document.SubDocType,
                    BoxOn = r.Document.BoxfileNo != null ? r.Document.BoxfileNo : null,
                    Notice = r.Document.Notes != null ? r.Document.Notes : null,
                    systemId = r.Document.RefrenceNo != null ? r.Document.RefrenceNo : null,
                    docsTitle = docTypeNames.ContainsKey(r.Document.DocType) ? docTypeNames[r.Document.DocType] : null,
                    HasJoinedDocs = r.HasJoinedDocs
                }).ToList();

                if (result == null || result.Count == 0)
                    return (null, "No documents found matching the criteria.");

                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error during search: {ex.Message}");
            }
        }


        //This method is used to search for deleted documents based ArchivingDocsDeleteds table.
        public async Task<List<BaseResponseDTOs>> DeletedDocsSearch(SearchDeletedDocsViewForm search)
        {
            // Get user ID
            var userIdResult = await _systemInfoServices.GetUserId();
            if (userIdResult.Id == null)
                return new List<BaseResponseDTOs> { new BaseResponseDTOs(null, 403, "User Id Not Found Please Login.") }
            ;// Unauthorized

            // Parse the user ID to integer
            if (!int.TryParse(userIdResult.Id, out int userIdInt))
                return new List<BaseResponseDTOs> { new BaseResponseDTOs(null, 403, "User Id Not Found Please Login.") };// Bad request - invalid user ID format

            // Get user details
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdInt);
            if (user == null)
               return new List<BaseResponseDTOs> { new BaseResponseDTOs(null, 403, "User Id Not Found Please Login.") };
            ; // User not found

            // Get user permissions
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(p => p.UserId == userIdInt);
            if (userPermissions == null)
                return new List<BaseResponseDTOs> { new BaseResponseDTOs(null, 403, "User Don't Have Any Permission.") };
                                                                                                                         
            // Check department access permission
            if (userPermissions.AllowViewTheOther == 0 && search.DepartId.HasValue && search.DepartId != user.DepariId)
            {
                // If user doesn't have general permission to view other departments,
                // check if they have specific permission for the requested department
                bool hasAccess = await _systemInfoServices.CheckUserHaveDepart(search.DepartId ?? 0, userIdInt);
                if (!hasAccess)
                return new List<BaseResponseDTOs> { new BaseResponseDTOs(null, 403, "User Id Not Found Please Login.") };// Forbidden - cannot view other departments
            }

            var query = _context.ArcivingDocsDeleteds.AsQueryable();

            // Filtering by account unit, branch, department
            if (search.accountUnitId.HasValue)
                query = query.Where(d => d.AccountUnitId == search.accountUnitId.Value);
            if (search.branchId.HasValue)
                query = query.Where(d => d.BranchId == search.branchId.Value);
            if (search.DepartId.HasValue)
                query = query.Where(d => d.DepartId == search.DepartId.Value);

            // Paging
            int pageNumber = search.pageList.HasValue && search.pageList.Value > 0 ? search.pageList.Value : 1;
            int pageSize = search.pageSize.HasValue && search.pageSize.Value > 0 ? search.pageSize.Value : 20;

            var pagedQuery = query
                .OrderByDescending(d => d.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            var result = await pagedQuery
                .Select(d => new DeletedDocsResponseDTOs
                {
                    Id = d.Id,
                    systemId = d.RefrenceNo,
                    docNO = d.DocNo,
                    docDate = d.DocDate,
                    source = d.DocSource != null ? d.DocSource.ToString() : null,
                    to = d.DocTarget != null ? d.DocTarget.ToString() : null,
                    subject = d.Subject,
                    docuType = d.DocType,
                    noitce = d.Notes,
                    editor = d.Editor,
                    editDocs = d.EditDate != null ? d.EditDate.ToString() : null
                })
                .ToListAsync();

            var response = result.Select(doc => new BaseResponseDTOs(data: doc, statusCode: 200)).ToList();

            return response;
        }


        //Note: this method is Nout used in the current codebase, but it is kept for case if we need it .
        public async Task<BaseResponseDTOs> PermissionSearch(UsersSearchViewForm search)
        {
            // Get user ID
            var userIdResult = await _systemInfoServices.GetUserId();
            if (userIdResult.Id == null)
                return new BaseResponseDTOs (null, 401 , "You must Login"); // Unauthorized

            // Parse the user ID to integer
            if (!int.TryParse(userIdResult.Id, out int userIdInt))
                return new BaseResponseDTOs (null, 400, "user Id format is invalid"); // Bad request - invalid user ID format

            // Get user details
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdInt);
            if (user == null)
                return new BaseResponseDTOs(null, 400, "user Id Not Found"); // User not found

            // Get user permissions
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(p => p.UserId == userIdInt);
            if (userPermissions == null)
                return new BaseResponseDTOs(null, 400, $"that User don't Have Any Permission"); // No permissions found

            // Check department access permission
            if (userPermissions.AllowViewTheOther == 0 && search.departmentId.HasValue && search.departmentId != user.DepariId)
            {
                // If user doesn't have general permission to view other departments,
                // check if they have specific permission for the requested department
                bool hasAccess = await _systemInfoServices.CheckUserHaveDepart(search.departmentId ?? 0, userIdInt);
                if (!hasAccess)
                    return new BaseResponseDTOs(null, 400, "user don't have permission to view Other Department"); // Forbidden - cannot view other departments
            }

            var query = _context.Users.AsQueryable();

            // Filtering by account unit, branch, department
            if (search.accountUnitId.HasValue)
                query = query.Where(d => d.AccountUnitId == search.accountUnitId.Value);
            if (search.branchId.HasValue)
                query = query.Where(d => d.BranchId == search.branchId.Value);
            if (search.departmentId.HasValue)
                query = query.Where(d => d.DepariId == search.departmentId.Value);


            int pageNumber = search.pageNumber != null && search.pageNumber > 0 ? search.pageNumber.Value : 1;
            int pageSize = search.pageSize != null && search.pageSize > 0 ? search.pageSize.Value : 20;


            var users = await query
            .OrderByDescending(d => d.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            var optionPermissions = await _context.UsersOptionPermissions
                .Where(p => userIds.Contains(p.UserId ?? 0))
                .ToListAsync();

            var archivingPoints = await _context.UsersArchivingPointsPermissions
                .Where(p => userIds.Contains(p.UserId ?? 0))
                .ToListAsync();
            var archivingPointIds = archivingPoints
                .Select(p => p.ArchivingpointId ?? 0)
                .ToList();

            var archivingPointNames = await _context.PArcivingPoints
                .Where(a => archivingPointIds.Contains(a.Id))
                .ToListAsync();
            var result = users.Select(d => new UsersSearchResponseDTOs
            {
                userId = d.Id,
                fileType = d.AsWfuser,
                usersOptionPermission = optionPermissions.FirstOrDefault(p => p.UserId == d.Id),
                archivingPoint = archivingPoints
                    .Where(p => p.UserId == d.Id)
                    .Select(p => new ArchivingPermissionResponseDTOs
                    {
                        archivingPointId = p.ArchivingpointId ?? 0,
                        archivingPointDscrp = archivingPointNames
                    .Where(a => a.Id == p.ArchivingpointId)
                    .Select(a => a.Dscrp)
                    .FirstOrDefault()
                    })
                    .ToList(),
            }).ToList();

            return new BaseResponseDTOs(result, 200);
        }

        public async Task<BaseResponseDTOs> SearchForJoinedDocs(string systemId)
        {
            var query = _context.JoinedDocs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(systemId))
            {
                query = query.Where(d => d.ParentRefrenceNO != null && d.ParentRefrenceNO == systemId);
            }

            var joined = await query
                .OrderByDescending(j => j.ParentRefrenceNO)
                .ToListAsync();

            // Get all child reference numbers
            var childRefs = joined.Select(j => j.ChildRefrenceNo).Distinct().ToList();

            // Query ArcivingDocs for details
            var docs = await _context.ArcivingDocs
                .Where(d => childRefs.Contains(d.RefrenceNo))
                .Select(d => new
                {
                    d.RefrenceNo,
                    systemId = d.ImgUrl,
                    d.ImgUrl,
                    d.DocNo,
                    d.DocDate,
                    d.DocTarget,
                    d.DocSource,
                    d.DocType,
                    d.BoxfileNo,
                    d.Subject
                })
                .ToListAsync();
            if (docs.Count == 0 || docs == null)
                return new BaseResponseDTOs(null, 404, "there is no Joined Docs");

            // Merge joined info with document details
            var result = joined.Select(j =>
            {
                var doc = docs.FirstOrDefault(d => d.RefrenceNo == j.ChildRefrenceNo);
                return new
                {
                    ParentRefrenceNO = j.ParentRefrenceNO,
                    ChildRefrenceNo = j.ChildRefrenceNo,
                    ImgUrl = doc.ImgUrl,
                    DocNo = doc?.DocNo,
                    DocDate = doc?.DocDate,
                    DocTarget = doc?.DocTarget,
                    DocSource = doc?.DocSource,
                    DocType = doc?.DocType,
                    BoxfileNo = doc?.BoxfileNo,
                    Subject = doc?.Subject
                };
            }).ToList();

            if (result == null || result.Count == 0)
                return new BaseResponseDTOs(null, 404, "No joined documents found.");

            return new BaseResponseDTOs(result, 200, null);
        }

        public async Task<BaseResponseDTOs> TreeSearch(TreeSearchViewForm req)
        {
            // Get user ID
            var userIdResult = await _systemInfoServices.GetUserId();
            if (userIdResult.Id == null)
                return new BaseResponseDTOs(null, 401, "You must Login"); // Unauthorized

            // Parse the user ID to integer
            if (!int.TryParse(userIdResult.Id, out int userIdInt))
                return new BaseResponseDTOs(null, 400, "user Id format is invalid"); // Bad request - invalid user ID format

            // Get user details
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdInt);
            if (user == null)
                return new BaseResponseDTOs(null, 400, "user Id Not Found"); // User not found

            // Get user permissions
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(p => p.UserId == userIdInt);
            if (userPermissions == null)
                return new BaseResponseDTOs(null, 400, $"that User don't Have Any Permission"); // No permissions found

            // Check department access permission
            if (userPermissions.AllowViewTheOther == 0 && req.departId.HasValue && req.departId != user.DepariId)
            {
                // If user doesn't have general permission to view other departments,
                // check if they have specific permission for the requested department
                bool hasAccess = await _systemInfoServices.CheckUserHaveDepart(req.departId ?? 0, userIdInt);
                if (!hasAccess)
                    return new BaseResponseDTOs(null, 400, "user don't have permission to view Other Department"); // Forbidden - cannot view other departments
            }

            var query = _context.ArcivingDocs.AsQueryable();

            // Filters
            if (req.from.HasValue)
                query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value >= DateOnly.FromDateTime(req.from.Value));
            if (req.to.HasValue)
                query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value <= DateOnly.FromDateTime(req.to.Value));
            if (!string.IsNullOrWhiteSpace(req.editor))
                query = query.Where(d => d.Editor != null && d.Editor.Contains(req.editor));
            if (req.docsType.HasValue)
                query = query.Where(d => d.DocType == req.docsType.Value);
            if (req.supDocsType.HasValue)
                query = query.Where(d => d.SubDocType.HasValue && d.SubDocType.Value == req.supDocsType.Value);
            if (req.source.HasValue)
                query = query.Where(d => d.DocSource.HasValue && d.DocSource.Value == req.source.Value);
            if (req.target.HasValue)
                query = query.Where(d => d.DocTarget.HasValue && d.DocTarget.Value == req.target.Value);
            if (!string.IsNullOrWhiteSpace(req.noitce))
                query = query.Where(d => d.DocTitle != null && d.DocTitle.Contains(req.noitce));
            if (req.departId.HasValue)
                query = query.Where(d => d.DepartId != null && d.DepartId == req.departId);

            // Pagination
            int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 20;
            int skip = (pageNumber - 1) * pageSize;

            // Get paged docs
            var pagedDocs = await query
                 .OrderByDescending(d => d.EditDate.HasValue ? d.EditDate.Value : DateTime .MinValue)
                 .ThenByDescending(d => d.DepartId)
                 .ThenByDescending(d => d.DocType)
                 .Skip(skip)
                 .Take(pageSize)
                 .ToListAsync();

            // Get department names and doc type names
            var departIds = pagedDocs.Select(d => d.DepartId).Distinct().ToList();
            var docTypeIds = pagedDocs.Select(d => d.DocType).Distinct().ToList();
            var departments = await _context.GpDepartments.Where(x => departIds.Contains(x.Id)).ToListAsync();
            var docTypes = await _context.ArcivDocDscrps.Where(x => docTypeIds.Contains(x.Id)).ToListAsync();

            // Group by department name, then doc type name, then year
            var grouped = pagedDocs
                .GroupBy(d => d.DepartId)
                .Select(deptGroup =>
                {
                    var departName = departments.FirstOrDefault(x => x.Id == deptGroup.Key)?.Dscrp ?? "Unknown";
                    return new
                    {
                        DepartId = deptGroup.Key,
                        DepartName = departName,
                        DocTypes = deptGroup
                            .GroupBy(d => d.DocType)
                            .Select(typeGroup =>
                            {
                                var docTypeName = docTypes.FirstOrDefault(x => x.Id == typeGroup.Key)?.Dscrp ?? "Unknown";
                                return new
                                {
                                    DocType = typeGroup.Key,
                                    DocTypeName = docTypeName,
                                    Years = typeGroup
                                        .GroupBy(d => d.EditDate.HasValue ? d.EditDate.Value.Year : 0)
                                        .Select(yearGroup => new
                                        {
                                            Year = yearGroup.Key,
                                            Documents = yearGroup.Select(d => new
                                            {
                                                Id = d.Id,
                                                ImgUrl = d.ImgUrl,
                                                Subject = d.Subject
                                            }).ToList()
                                        }).OrderBy(y => y.Year).ToList()
                                };
                            }).ToList()
                    };
                }).ToList();

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new BaseResponseDTOs(new
            {
                Data = grouped,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, 200, null);
        }

        public async Task<BaseResponseDTOs> CasesSearch(CasesSearchViewForm req)
        {
            var query = _context.JoinedDocs.AsQueryable();

            // Fixed the condition - only filter when CaseNumber is NOT empty
            if (!string.IsNullOrEmpty(req.CaseNumber))
                query = query.Where(q => q.BreafcaseNo != null && q.BreafcaseNo.Contains(req.CaseNumber));

            if (!string.IsNullOrEmpty(req.ReferenceNumber))
                query = query.Where(q => q.ParentRefrenceNO == req.ReferenceNumber || q.ChildRefrenceNo == req.ReferenceNumber);
            if (req.from.HasValue)
                query = query.Where(d => d.editDate.HasValue && d.editDate.Value >= req.from);
            if (req.to.HasValue)
                query = query.Where(d => d.editDate.HasValue && d.editDate.Value <= req.to);

            var pagedList = await PagedList<T_JoinedDoc>.Create(
                query.OrderBy(d => d.ParentRefrenceNO),
                req.pageNumber,
                req.pageSize);

            // Get all referenced documents to fetch their image paths
            var allRefNos = pagedList.Items
                .SelectMany(d => new[] { d.ParentRefrenceNO, d.ChildRefrenceNo })
                .Where(refNo => !string.IsNullOrEmpty(refNo))
                .Distinct()
                .ToList();

            // Get document details including image paths
            var docDetails = await _context.ArcivingDocs
                .Where(d => allRefNos.Contains(d.RefrenceNo))
                .Select(d => new { d.RefrenceNo, d.ImgUrl })
                .ToDictionaryAsync(d => d.RefrenceNo, d => d.ImgUrl);

            var groupedResult = pagedList.Items
               .GroupBy(d => d.ParentRefrenceNO)
               .Select(g => new
               {
                   ParentRefrenceNO = g.Key,
                   imgUrl = docDetails.TryGetValue(g.Key, out var parentImgUrl) ? parentImgUrl : null,
                   // Include BreafcaseNo at the parent level - using the first item in the group
                   BreafcaseNo = g.FirstOrDefault()?.BreafcaseNo,
                   ChildDocuments = g.Select(j => new
                   {
                       j.ChildRefrenceNo,
                       j.BreafcaseNo,
                       j.editDate,
                       imgUrl = docDetails.TryGetValue(j.ChildRefrenceNo, out var childImgUrl) ? childImgUrl : null
                   }).ToList()
               })
               .ToList();

            return new BaseResponseDTOs(new
            {
                Data = groupedResult,
                TotalCount = pagedList.TotalCount,
                TotalPages = pagedList.TotalPages,
                PageNumber = pagedList.PageNumber,
                PageSize = pagedList.PageSize
            }, 200, null);
        }

        public async Task<BaseResponseDTOs> AzberSearch(string azberNo)
        {
            if (string.IsNullOrEmpty(azberNo))
            {
                var azbers = await _context.JoinedDocs.ToListAsync();
                if (azbers.Count == 0 || !azbers.Any())
                    return new BaseResponseDTOs(null, 400, "There is No Azbera Number");

                return new BaseResponseDTOs(azbers, 200, null);
            }

            var azber = await _context.JoinedDocs.FirstOrDefaultAsync(a => a.BreafcaseNo == azberNo);
            if (azber == null)
                return new BaseResponseDTOs(null, 404, $"No case found with Azbera number: {azberNo}");

            return new BaseResponseDTOs(azber, 200, null);
        }

        public async Task<BaseResponseDTOs> SearchForJoinedDocsFilter(QuikeSearchViewForm req)
        {
            // Get user ID
            var userIdResult = await _systemInfoServices.GetUserId();
            if (userIdResult.Id == null)
                return new BaseResponseDTOs(null, 401, "You must Login"); // Unauthorized

            // Parse the user ID to integer
            if (!int.TryParse(userIdResult.Id, out int userIdInt))
                return new BaseResponseDTOs(null, 400, "user Id format is invalid"); // Bad request - invalid user ID format

            // Get user details
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdInt);
            if (user == null)
                return new BaseResponseDTOs(null, 400, "user Id Not Found"); // User not found

            // Get user permissions
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(p => p.UserId == userIdInt);
            if (userPermissions == null)
                return new BaseResponseDTOs(null, 400, $"that User don't Have Any Permission"); // No permissions found

            // Check department access permission
            if (userPermissions.AllowViewTheOther == 0 && req.departId.HasValue && req.departId != user.DepariId)
            {
                // If user doesn't have general permission to view other departments,
                // check if they have specific permission for the requested department
                bool hasAccess = await _systemInfoServices.CheckUserHaveDepart(req.departId ?? 0, userIdInt);
                if (!hasAccess)
                    return new BaseResponseDTOs(null, 400, "user don't have permission to view Other Department"); // Forbidden - cannot view other departments
            }

            var query = _context.ArcivingDocs.AsQueryable();

            // Filter for docs that have ReferenceTo set (not null or empty)
            query = query.Where(d => !string.IsNullOrEmpty(d.ReferenceTo));

            // Apply all possible filters from QuikeSearchViewForm
            // Document filters
            if (!string.IsNullOrWhiteSpace(req.systemId))
                query = query.Where(d => d.RefrenceNo != null && d.RefrenceNo.Contains(req.systemId));

            if (!string.IsNullOrWhiteSpace(req.docsNumber))
            {
                if (req.exactMatch)
                    query = query.Where(d => d.DocNo != null && d.DocNo == req.docsNumber);
                else
                    query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));
            }

            if (!string.IsNullOrWhiteSpace(req.subject))
                query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));

            if (req.docsType.HasValue)
                query = query.Where(d => d.DocType == req.docsType.Value);

            if (req.supDocsType.HasValue)
                query = query.Where(d => d.SubDocType.HasValue && d.SubDocType.Value == req.supDocsType.Value);

            if (req.source.HasValue)
                query = query.Where(d => d.DocSource != null && d.DocSource.Value == req.source.Value);

            // Check for related document references
            if (!string.IsNullOrEmpty(req.relateTo))
            {
                // Look for documents that either:
                // 1. Have the relateTo value directly in their ReferenceTo field
                // 2. Have an entry in the JoinedDocs table where this document is the parent
                var joinedDocRefs = _context.JoinedDocs
                    .Where(j => j.ParentRefrenceNO == req.relateTo)
                    .Select(j => j.ChildRefrenceNo)
                    .ToList();

                if (joinedDocRefs.Any())
                {
                    // If we found related documents in JoinedDocs, include documents with matching RefrenceNo
                    query = query.Where(d =>
                        (d.ReferenceTo != null && d.ReferenceTo.Contains(req.relateTo)) ||
                        joinedDocRefs.Contains(d.RefrenceNo));
                }
                else
                {
                    // If no joined documents found, just check the ReferenceTo field
                    query = query.Where(d => d.ReferenceTo != null && d.ReferenceTo.Contains(req.relateTo));
                }
            }

            if (!string.IsNullOrWhiteSpace(req.wordToSearch))
                query = query.Where(d => d.Notes != null && d.Notes.Contains(req.wordToSearch));

            if (!string.IsNullOrWhiteSpace(req.boxFile))
                query = query.Where(d => d.BoxfileNo != null && d.BoxfileNo.Contains(req.boxFile));

            if (req.fileType.HasValue)
                query = query.Where(d => d.FileType.HasValue && d.FileType.Value == (int)req.fileType.Value);

            if (req.departId.HasValue)
                query = query.Where(d => d.DepartId.HasValue && d.DepartId.Value == req.departId.Value);

            // Date filters
            if (req.docsDate == true)
            {
                if (req.from.HasValue)
                    query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value >= DateOnly.FromDateTime(req.from.Value));
                if (req.to.HasValue)
                    query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value <= DateOnly.FromDateTime(req.to.Value));
            }

            if (req.editDate == true)
            {
                if (req.from.HasValue)
                    query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value >= req.from.Value);
                if (req.to.HasValue)
                    query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value <= req.to.Value);
            }

            // Paging
            int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 20;

            // Count total results for pagination info
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Get paged results
            var pagedDocs = await query
                .OrderByDescending(d => d.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get document types for names
            var docTypeIds = pagedDocs.Select(d => d.DocType).Distinct().ToList();
            var docTypes = await _context.ArcivDocDscrps
                .Where(dt => docTypeIds.Contains(dt.Id))
                .ToDictionaryAsync(dt => dt.Id, dt => dt.Dscrp);

            // Get reference information from JoinedDocs table
            var refNos = pagedDocs.Select(d => d.RefrenceNo).ToList();
            var joinedDocs = await _context.JoinedDocs
                .Where(j => refNos.Contains(j.ChildRefrenceNo))
                .ToDictionaryAsync(j => j.ChildRefrenceNo, j => j.ParentRefrenceNO);

            // Collect all parent reference numbers
            var allParentRefNos = new HashSet<string>();

            foreach (var doc in pagedDocs)
            {
                if (joinedDocs.TryGetValue(doc.RefrenceNo, out var parentRef))
                {
                    allParentRefNos.Add(parentRef);
                }
                else if (!string.IsNullOrEmpty(doc.ReferenceTo))
                {
                    allParentRefNos.Add(doc.ReferenceTo);
                }
            }

            // Get parent document details
            var parentDocs = await _context.ArcivingDocs
                .Where(d => allParentRefNos.Contains(d.RefrenceNo))
                .ToListAsync();

            // Create a dictionary for quick lookup of parent documents by reference number
            var parentDocsByRef = parentDocs.ToDictionary(d => d.RefrenceNo, d => d);

            // Get parent document type names
            var parentDocTypeIds = parentDocs.Select(d => d.DocType).Distinct().ToList();
            var parentDocTypes = await _context.ArcivDocDscrps
                .Where(dt => parentDocTypeIds.Contains(dt.Id))
                .ToDictionaryAsync(dt => dt.Id, dt => dt.Dscrp);

            // Prepare the result items with parent reference information
            var resultItems = pagedDocs.Select(d =>
            {
                // Determine parent reference number
                string parentRefNo = joinedDocs.TryGetValue(d.RefrenceNo, out var joinedParentRef)
                    ? joinedParentRef
                    : d.ReferenceTo;

                // Try to get parent document
                bool hasParentDoc = parentDocsByRef.TryGetValue(parentRefNo, out var parentDoc);

                return new
                {
                    d.Id,
                    d.RefrenceNo,
                    d.ImgUrl,
                    d.DocNo,
                    d.DocDate,
                    d.DocTarget,
                    d.DocSource,
                    d.DocType,
                    DocTypeName = docTypes.TryGetValue(d.DocType, out var typeName) ? typeName : null,
                    d.BoxfileNo,
                    d.Subject,
                    ParentReferenceNo = parentRefNo,
                    ParentDocument = hasParentDoc ? new
                    {
                        parentDoc.Id,
                        parentDoc.RefrenceNo,
                        parentDoc.ImgUrl,
                        parentDoc.DocNo,
                        parentDoc.DocDate,
                        parentDoc.DocTarget,
                        parentDoc.DocSource,
                        parentDoc.DocType,
                        DocTypeName = parentDocTypes.TryGetValue(parentDoc.DocType, out var parentTypeName) ? parentTypeName : null,
                        parentDoc.BoxfileNo,
                        parentDoc.Subject
                    } : null
                };
            }).ToList();

            if (resultItems.Count == 0)
                return new BaseResponseDTOs(null, 404, "No documents found with ReferenceTo matching your criteria.");

            // Group results by parent reference number
            var groupedResult = resultItems
                .GroupBy(item => item.ParentReferenceNo)
                .OrderBy(group => group.Key)
                .Select(group => new
                {
                    ParentReferenceNo = group.Key,
                    ParentDocument = group.First().ParentDocument, // Include parent document information
                    Documents = group.Select(doc => new
                    {
                        doc.Id,
                        doc.RefrenceNo,
                        doc.ImgUrl,
                        doc.DocNo,
                        doc.DocDate,
                        doc.DocTarget,
                        doc.DocSource,
                        doc.DocType,
                        doc.DocTypeName,
                        doc.BoxfileNo,
                        doc.Subject
                    }).ToList()
                })
                .ToList();

            return new BaseResponseDTOs(
                new
                {
                    Data = groupedResult,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                },
                200,
                null
            );
        }

        public async Task<BaseResponseDTOs> SearchForAllJoinedDocs(QuikeSearchViewForm req)
        {
            // Get user ID
            var userIdResult = await _systemInfoServices.GetUserId();
            if (userIdResult.Id == null)
                return new BaseResponseDTOs(null, 401, "You must Login"); // Unauthorized

            // Parse the user ID to integer
            if (!int.TryParse(userIdResult.Id, out int userIdInt))
                return new BaseResponseDTOs(null, 400, "user Id format is invalid"); // Bad request - invalid user ID format

            // Get user details
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdInt);
            if (user == null)
                return new BaseResponseDTOs(null, 400, "user Id Not Found"); // User not found

            // Get user permissions
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(p => p.UserId == userIdInt);
            if (userPermissions == null)
                return new BaseResponseDTOs(null, 400, $"that User don't Have Any Permission"); // No permissions found

            // Check department access permission
            if (userPermissions.AllowViewTheOther == 0 && req.departId.HasValue && req.departId != user.DepariId)
            {
                // If user doesn't have general permission to view other departments,
                // check if they have specific permission for the requested department
                bool hasAccess = await _systemInfoServices.CheckUserHaveDepart(req.departId ?? 0, userIdInt);
                if (!hasAccess)
                    return new BaseResponseDTOs(null, 400, "user don't have permission to view Other Department"); // Forbidden - cannot view other departments
            }

            // Start with JoinedDocs query to get all joined documents
            var joinedDocsQuery = _context.JoinedDocs.AsQueryable();

            // Apply filters to the joined documents based on search criteria
            if (!string.IsNullOrEmpty(req.relateTo))
            {
                joinedDocsQuery = joinedDocsQuery.Where(j => 
                    j.ParentRefrenceNO == req.relateTo || 
                    j.ChildRefrenceNo == req.relateTo);
            }

            // Date filters on joined docs
            if (req.editDate == true)
            {
                if (req.from.HasValue)
                    joinedDocsQuery = joinedDocsQuery.Where(j => j.editDate.HasValue && j.editDate.Value >= req.from.Value);
                if (req.to.HasValue)
                    joinedDocsQuery = joinedDocsQuery.Where(j => j.editDate.HasValue && j.editDate.Value <= req.to.Value);
            }

            // Apply paging
            int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 20;

            // Count total results for pagination info
            int totalCount = await joinedDocsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Get paged joined documents
            var pagedJoinedDocs = await joinedDocsQuery
                .OrderByDescending(j => j.ParentRefrenceNO)
                .ThenByDescending(j => j.editDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (pagedJoinedDocs.Count == 0)
                return new BaseResponseDTOs(null, 404, "No joined documents found matching your criteria.");

            // Get all child reference numbers to fetch document details
            var childRefNos = pagedJoinedDocs.Select(j => j.ChildRefrenceNo).Distinct().ToList();

            // Get child document details from ArcivingDocs
            var childDocs = await _context.ArcivingDocs
                .Where(d => childRefNos.Contains(d.RefrenceNo))
                .ToListAsync();

            // Create a dictionary for quick lookup of child documents by reference number
            var childDocsByRef = childDocs.ToDictionary(d => d.RefrenceNo, d => d);

            // Get document types for names
            var docTypeIds = childDocs.Select(d => d.DocType).Distinct().ToList();
            var docTypes = await _context.ArcivDocDscrps
                .Where(dt => docTypeIds.Contains(dt.Id))
                .ToDictionaryAsync(dt => dt.Id, dt => dt.Dscrp);

            // Apply additional filters on child documents if needed
            var filteredJoinedDocs = pagedJoinedDocs.Where(j =>
            {
                if (!childDocsByRef.TryGetValue(j.ChildRefrenceNo, out var childDoc))
                    return false;

                // Apply document-specific filters
                if (!string.IsNullOrWhiteSpace(req.systemId) && !childDoc.RefrenceNo.Contains(req.systemId))
                    return false;

                if (!string.IsNullOrWhiteSpace(req.docsNumber))
                {
                    if (req.exactMatch && childDoc.DocNo != req.docsNumber)
                        return false;
                    else if (!req.exactMatch && (childDoc.DocNo == null || !childDoc.DocNo.Contains(req.docsNumber)))
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(req.subject) && 
                    (childDoc.Subject == null || !childDoc.Subject.Contains(req.subject)))
                    return false;

                if (req.docsType.HasValue && childDoc.DocType != req.docsType.Value)
                    return false;

                if (req.supDocsType.HasValue && 
                    (!childDoc.SubDocType.HasValue || childDoc.SubDocType.Value != req.supDocsType.Value))
                    return false;

                if (req.source.HasValue && 
                    (!childDoc.DocSource.HasValue || childDoc.DocSource.Value != req.source.Value))
                    return false;

                if (!string.IsNullOrWhiteSpace(req.wordToSearch) && 
                    (childDoc.Notes == null || !childDoc.Notes.Contains(req.wordToSearch)))
                    return false;

                if (!string.IsNullOrWhiteSpace(req.boxFile) && 
                    (childDoc.BoxfileNo == null || !childDoc.BoxfileNo.Contains(req.boxFile)))
                    return false;

                if (req.fileType.HasValue && 
                    (!childDoc.FileType.HasValue || childDoc.FileType.Value != (int)req.fileType.Value))
                    return false;

                if (req.departId.HasValue && 
                    (!childDoc.DepartId.HasValue || childDoc.DepartId.Value != req.departId.Value))
                    return false;

                // Document date filters
                if (req.docsDate == true)
                {
                    if (req.from.HasValue && 
                        (!childDoc.DocDate.HasValue || childDoc.DocDate.Value < DateOnly.FromDateTime(req.from.Value)))
                        return false;
                    if (req.to.HasValue && 
                        (!childDoc.DocDate.HasValue || childDoc.DocDate.Value > DateOnly.FromDateTime(req.to.Value)))
                        return false;
                }

                return true;
            }).ToList();

            // Group results by parent reference number
            var groupedResult = filteredJoinedDocs
                .GroupBy(j => j.ParentRefrenceNO)
                .OrderBy(group => group.Key)
                .Select(group => new
                {
                    ParentReferenceNo = group.Key,
                    BreafcaseNo = group.First().BreafcaseNo, // Include briefcase number from JoinedDocs table
                    Documents = group.Select(j =>
                    {
                        var childDoc = childDocsByRef.TryGetValue(j.ChildRefrenceNo, out var doc) ? doc : null;
                        return new
                        {
                            Id = childDoc?.Id,
                            RefrenceNo = j.ChildRefrenceNo,
                            ImgUrl = childDoc?.ImgUrl,
                            DocNo = childDoc?.DocNo,
                            DocDate = childDoc?.DocDate,
                            DocTarget = childDoc?.DocTarget,
                            DocSource = childDoc?.DocSource,
                            DocType = childDoc?.DocType,
                            DocTypeName = childDoc != null && docTypes.TryGetValue(childDoc.DocType, out var typeName) ? typeName : null,
                            BoxfileNo = childDoc?.BoxfileNo,
                            Subject = childDoc?.Subject,
                            EditDate = j.editDate
                        };
                    }).ToList()
                })
                .ToList();

            return new BaseResponseDTOs(
                new
                {
                    Data = groupedResult,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                },
                200,
                null
            );
        }

        // ...existing methods...
    }
}