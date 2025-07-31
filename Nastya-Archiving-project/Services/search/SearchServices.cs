using AutoMapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using FYP.Extentions;
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
using Org.BouncyCastle.Ocsp;

namespace Nastya_Archiving_project.Services.search
{
    public class SearchServices :  ISearchServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IEncryptionServices _encryptionServices;
        public SearchServices(AppDbContext context, IMapper mapper, IEncryptionServices encryptionServices)
        {
            _context = context;
            _mapper = mapper;
            _encryptionServices = encryptionServices;
        }

        //public async Task<(List<QuikSearchResponseDTOs>? docs, string? error)> QuikeSearch(QuikeSearchViewForm req)
        //{
        //    try
        //    {
        //        var query = _context.ArcivingDocs.AsQueryable();

        //        // Map QuikeSearchViewForm to BaseFilter
        //        var filter = new BaseFilter
        //        {
        //            Id = null, // Set if you want to filter by Id
        //            IsDeleted = null, // Set if you want to filter by IsDeleted
        //            StartDate = req.docsDate == true && req.from.HasValue ? req.from : null,
        //            EndDate = req.docsDate == true && req.to.HasValue ? req.to : null
        //        };

        //        // Apply base filter using extension method
        //        query = query.WhereBaseFilter(filter);

        //        // Custom filters for fields not covered by BaseFilter
        //        if (!string.IsNullOrWhiteSpace(req.docsNumber))
        //            query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));


        //        // Check if editDate filter is applied
        //        if (req.editDate == true)
        //        {
        //            if (req.from.HasValue)
        //                query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value >= req.from.Value);
        //            if (req.to.HasValue)
        //                query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value <= req.to.Value);
        //        }

        //        // Check if docsDate filter is applied
        //        if (req.docsDate == true)
        //        {
        //            if (req.from.HasValue)
        //                query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value >= DateOnly.FromDateTime(req.from.Value));
        //            if (req.to.HasValue)
        //                query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value <= DateOnly.FromDateTime(req.to.Value));
        //        }


        //        if (!string.IsNullOrWhiteSpace(req.subject))
        //            query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));

        //        if (req.source.HasValue)
        //            query = query.Where(d => d.DocSource != null && d.DocSource.Value == (int)req.source);

        //        if(req.departId.HasValue)
        //            query = query.Where(d => d.DepartId.HasValue && d.DepartId.Value == req.departId.Value);

        //        if (req.ReferenceTo.HasValue)
        //            query = query.Where(d => d.ReferenceTo != null && d.DocTarget.Value == (int)req.ReferenceTo);

        //        if (req.fileType.HasValue)
        //            query = query.Where(d => d.FileType.HasValue && d.FileType.Value == (int)req.fileType.Value);

        //        // Paging
        //        int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
        //        int pageSize = req.pageSize > 0 ? req.pageSize : 20;

        //        var pagedQuery = query
        //            .OrderByDescending(d => d.Id)
        //            .Skip((pageNumber - 1) * pageSize)
        //            .Take(pageSize);

        //        var result = await pagedQuery.Select(d => new QuikSearchResponseDTOs
        //        {
        //            systemId = d.RefrenceNo,
        //            Id = d.Id,
        //            file = d.ImgUrl,
        //            editDate = d.EditDate,
        //            docsNumber = d.DocNo,
        //            docsDate = d.DocDate.HasValue ? d.DocDate.Value : null,
        //            subject = d.Subject,
        //            source = d.DocSource != null ? d.DocSource.ToString() : null,
        //            ReferenceTo = d.ReferenceTo,
        //            fileType = d.FileType != null ? d.FileType.ToString() : null
        //        }).ToListAsync();

        //        if (result == null || result.Count == 0)
        //            return (null, "No documents found matching the criteria.");

        //        return (result, null);
        //    }
        //    catch (Exception ex)
        //    {
        //        return (null, $"Error during search: {ex.Message}");
        //    }
        //}
        public async Task<(List<QuikSearchResponseDTOs>? docs, string? error)> QuikeSearch(QuikeSearchViewForm req)
        {
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
                    query = query.Where(d => d.DocType == req.docsType.Value);
                }
                // ArcivingDoc does not have SupDocType, so skip this filter

                if (!string.IsNullOrWhiteSpace(req.wordToSearch))
                    query = query.Where(d => d.WordsTosearch != null && d.WordsTosearch.Contains(req.wordToSearch));
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

                var pagedDocs = await query
                    .OrderByDescending(d => d.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get all doc types for mapping id to name
                var docTypeIds = pagedDocs.Select(d => d.DocType).Distinct().ToList();
                var docTypes = await _context.ArcivDocDscrps
                    .Where(dt => docTypeIds.Contains(dt.Id))
                    .ToListAsync();
                var docTypeNames = docTypes.ToDictionary(x => x.Id, x => x.Dscrp);

                var result = pagedDocs.Select(d => new QuikSearchResponseDTOs
                {
                    systemId = d.RefrenceNo,
                    Id = d.Id,
                    file = d.ImgUrl,
                    editDate = d.EditDate,
                    docsNumber = d.DocNo,
                    docsDate = d.DocDate.HasValue ? d.DocDate.Value : null,
                    subject = d.Subject,
                    source = d.DocSource != null ? d.DocSource.ToString() : null,
                    ReferenceTo = d.ReferenceTo,
                    fileType = d.FileType != null ? d.FileType.ToString() : null,
                    // Add doctypeName to the response
                    docsTitle = docTypeNames.ContainsKey(d.DocType) ? docTypeNames[d.DocType] : null
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

                if (!string.IsNullOrWhiteSpace(req.wordToSearch))
                    query = query.Where(d => d.WordsTosearch != null && d.WordsTosearch.Contains(req.wordToSearch));
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

                var pagedQuery = query
                    .OrderByDescending(d => d.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);

                // Get all doc types for mapping id to name
                var pagedDocs = await pagedQuery.ToListAsync();
                var docTypeIds = pagedDocs.Select(d => d.DocType).Distinct().ToList();
                var docTypes = await _context.ArcivDocDscrps
                    .Where(dt => docTypeIds.Contains(dt.Id))
                    .ToListAsync();
                var docTypeNames = docTypes.ToDictionary(x => x.Id, x => x.Dscrp);

                var result = pagedDocs.Select(d => new DetialisSearchResponseDTOs
                {
                    Id = d.Id,
                    file = d.ImgUrl,
                    docsNumber = d.DocNo,
                    docsDate = d.DocDate.HasValue ? d.DocDate.Value : null,
                    editDate = d.EditDate.HasValue ? d.EditDate.Value : null,
                    subject = d.Subject,
                    source = d.DocSource != null ? d.DocSource.ToString() : null,
                    ReferenceTo = d.ReferenceTo,
                    fileType = d.FileType != null ? d.FileType.ToString() : null,
                    BoxOn = d.BoxfileNo != null ? d.BoxfileNo : null,
                    Notice = d.WordsTosearch != null ? d.WordsTosearch : null,
                    systemId = d.RefrenceNo != null ? d.RefrenceNo : null,
                    docsTitle = docTypeNames.ContainsKey(d.DocType) ? docTypeNames[d.DocType] : null // Add docType name
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
        public async Task<BaseResponseDTOs> SearchForJoinedDocsFilter(QuikeSearchViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Filter for docs that have ReferenceTo set (not null or empty)
            query = query.Where(d => !string.IsNullOrEmpty(d.ReferenceTo));

            // Apply additional filters from QuikeSearchViewForm if needed
            if (!string.IsNullOrWhiteSpace(req.docsNumber))
                query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));
            if (!string.IsNullOrWhiteSpace(req.subject))
                query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));
            if (req.docsType.HasValue)
                query = query.Where(d => d.DocType == req.docsType.Value);
            if (req.source.HasValue)
                query = query.Where(d => d.DocSource != null && d.DocSource.Value == req.source.Value);
            if (!string.IsNullOrWhiteSpace(req.wordToSearch))
                query = query.Where(d => d.WordsTosearch != null && d.WordsTosearch.Contains(req.wordToSearch));

            // Paging
            int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 20;

            var pagedDocs = await query
                .OrderByDescending(d => d.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = pagedDocs.Select(d => new
            {
                d.Id,
                d.DocNo,
                d.DocDate,
                d.DocTarget,
                d.DocSource,
                d.DocType,
                d.BoxfileNo,
                d.Subject,
                d.ReferenceTo
            }).ToList();

            if (result.Count == 0)
                return new BaseResponseDTOs(null, 404, "No documents found with ReferenceTo.");

            return new BaseResponseDTOs(result, 200, null);
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
                    d.DocNo,
                    d.DocDate,
                    d.DocTarget,
                    d.DocSource,
                    d.DocType,
                    d.BoxfileNo,
                    d.Subject
                })
                .ToListAsync();

            // Merge joined info with document details
            var result = joined.Select(j =>
            {
                var doc = docs.FirstOrDefault(d => d.RefrenceNo == j.ChildRefrenceNo);
                return new
                {
                    ParentRefrenceNO = j.ParentRefrenceNO,
                    ChildRefrenceNo = j.ChildRefrenceNo,
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

            // Pagination
            int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 20;
            int skip = (pageNumber - 1) * pageSize;

            // Get paged docs
            var pagedDocs = await query
                .OrderBy(d => d.DepartId)
                .ThenBy(d => d.DocType)
                .ThenBy(d => d.DocDate.HasValue ? d.DocDate.Value.Year : 0)
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

            if(req.CaseNumber.HasValue)
                query = query.Where(q => q.BreafcaseNo ==  req.CaseNumber.Value);
            if (req.from.HasValue)
                query = query.Where(d => d.editDate.HasValue && d.editDate.Value >= req.from);
            if (req.to.HasValue)
                query = query.Where(d => d.editDate.HasValue && d.editDate.Value <= req.to);


            var pagedList = await PagedList<T_JoinedDoc>.Create(
                query.OrderBy(d => d.ParentRefrenceNO),
                req.pageNumber,
                req.pageSize);

            var groupedResult = pagedList.Items
               .GroupBy(d => d.ParentRefrenceNO)
               .Select(g => new
               {
                   ParentRefrenceNO = g.Key,
                   ChildDocuments = g.Select(j => new
                   {
                       j.ChildRefrenceNo,
                       j.BreafcaseNo,
                       j.editDate
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

    }
}
