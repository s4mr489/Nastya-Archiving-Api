using AutoMapper;
using FYP.Extentions;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Helper.Enums;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search;
using Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch;
using Nastya_Archiving_project.Models.DTOs.Search.QuikSearch;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;

namespace Nastya_Archiving_project.Services.search
{
    public class SearchServices :  ISearchServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        public SearchServices(AppDbContext context, IMapper mapper) 
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<(List<QuikSearchResponseDTOs>? docs, string? error)> QuikeSearch(QuikeSearchViewForm req)
        {
            try
            {
                var query = _context.ArcivingDocs.AsQueryable();

                // Map QuikeSearchViewForm to BaseFilter
                var filter = new BaseFilter
                {
                    Id = null, // Set if you want to filter by Id
                    IsDeleted = null, // Set if you want to filter by IsDeleted
                    StartDate = req.docsDate == true && req.from.HasValue ? req.from : null,
                    EndDate = req.docsDate == true && req.to.HasValue ? req.to : null
                };

                // Apply base filter using extension method
                query = query.WhereBaseFilter(filter);

                // Custom filters for fields not covered by BaseFilter
                if (!string.IsNullOrWhiteSpace(req.docsNumber))
                    query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));


                // Check if editDate filter is applied
                if (req.editDate == true)
                {
                    if (req.from.HasValue)
                        query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value >= req.from.Value);
                    if (req.to.HasValue)
                        query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value <= req.to.Value);
                }

                // Check if docsDate filter is applied
                if (req.docsDate == true)
                {
                    if (req.from.HasValue)
                        query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value >= DateOnly.FromDateTime(req.from.Value));
                    if (req.to.HasValue)
                        query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value <= DateOnly.FromDateTime(req.to.Value));
                }


                if (!string.IsNullOrWhiteSpace(req.subject))
                    query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));

                if (!string.IsNullOrWhiteSpace(req.source))
                    query = query.Where(d => d.DocSource != null && d.DocSource.ToString().Contains(req.source));

                if (!string.IsNullOrWhiteSpace(req.ReferenceTo))
                    query = query.Where(d => d.ReferenceTo != null && d.ReferenceTo.Contains(req.ReferenceTo));

                if (req.fileType.HasValue)
                    query = query.Where(d => d.FileType.HasValue && d.FileType.Value == (int)req.fileType.Value);

                // Paging
                int pageNumber = req.pageNumber > 0 ? req.pageNumber : 1;
                int pageSize = req.pageSize > 0 ? req.pageSize : 20;

                var pagedQuery = query
                    .OrderByDescending(d => d.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);

                var result = await pagedQuery.Select(d => new QuikSearchResponseDTOs
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
                    fileType = d.FileType != null ? d.FileType.ToString() : null
                }).ToListAsync();

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

                // Custom filters not covered by BaseFilter
                if (!string.IsNullOrWhiteSpace(req.docsNumber))
                    query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));
                if (!string.IsNullOrWhiteSpace(req.subject))
                    query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));
                if (!string.IsNullOrWhiteSpace(req.systemId))
                    query = query.Where(d => d.SystemId != null && d.SystemId.Contains(req.systemId));
                // ... (repeat for other custom fields)

                // Paging and ordering
                var pagedQuery = query
                    .OrderByDescending(d => d.Id)
                    .Skip((req.pageNumber - 1) * req.pageSize)
                    .Take(req.pageSize);

                var result = await pagedQuery.Select(d => new DetialisSearchResponseDTOs
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
                    systemId = d.RefrenceNo != null ? d.RefrenceNo: null
                }).ToListAsync();

                if (result == null || result.Count == 0)
                    return (null, "No documents found matching the criteria.");

                return (result, null);
            }
            catch (Exception ex)
            {
                return (null, $"Error during search: {ex.Message}");
            }
        }

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

        //public async Task<BaseResponseDTOs> PermissionSearch(UsersSearchViewForm search)
        //{
        //    var query = _context.Users.AsQueryable();

        //    // Filtering by account unit, branch, department
        //    if (search.accountUnitId.HasValue)
        //        query = query.Where(d => d.AccountUnitId == search.accountUnitId.Value);
        //    if (search.branchId.HasValue)
        //        query = query.Where(d => d.BranchId == search.branchId.Value);
        //    if (search.departmentId.HasValue)
        //        query = query.Where(d => d.DepariId == search.departmentId.Value);

        //    int pageNumber = search.pageNumber != null && search.pageNumber > 0 ? search.pageNumber.Value : 1;
        //    int pageSize = search.pageSize != null && search.pageSize > 0 ? search.pageSize.Value : 20;

        //    var users = await query
        //        .OrderByDescending(d => d.Id)
        //        .Skip((pageNumber - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToListAsync();

        //    var userIds = users.Select(u => u.Id).ToList();

        //    var optionPermissions = await _context.UsersOptionPermissions
        //        .Where(p => userIds.Contains(p.UserId ?? 0))
        //        .ToListAsync();

        //    var archivingPoints = await _context.UsersArchivingPointsPermissions
        //        .Where(p => userIds.Contains(p.UserId ?? 0))
        //        .ToListAsync();

        //    var result = users.Select(d => new UsersSearchResponseDTOs
        //    {
        //        userId = d.Id,
        //        fileType = d.AsWfuser,
        //        usersOptionPermission = optionPermissions.FirstOrDefault(p => p.UserId == d.Id),
        //        archivingPoint = archivingPoints
        //            .Where(p => p.UserId == d.Id)
        //            .ToList()
        //    }).ToList();

        //    // Return all users as a list inside a single BaseResponseDTOs
        //    return new BaseResponseDTOs(result, 200);
        //}


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
    }
}
