using AutoMapper;
using FYP.Extentions;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search;
using Nastya_Archiving_project.Models.DTOs.Search.DeletedDocsSearch;
using Nastya_Archiving_project.Models.DTOs.Search.QuikSearch;
using System.Linq.Expressions;
using System.Net.Quic;

namespace Nastya_Archiving_project.Services.search
{
    public class SearchServices : BaseServices, ISearchServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        public SearchServices(AppDbContext context, IMapper mapper) : base(mapper, context)
        {
            _context = context;
            _mapper = mapper;
        }
        // idont use keep just to read from it and take information about the methods
        ////Quick search method Using IQueryable for better performance and flexibility
        //public async Task<(List<QuikSearchResponseDTOs>? docs, string? error)> QuikeSearch(QuikeSearchViewForm req)
        //{
        //    try
        //    {
        //        // Default pagination values
        //        //int pageNumber = req.pageNumber > 0 ? req.pageNumber: 1;
        //        //int pageSize = req.pageNumber > 0 ? req.pageNumber : 20;

        //        var query = _context.ArcivingDocs.AsQueryable();

        //        // Filtering logic
        //        if (!string.IsNullOrWhiteSpace(req.docsNumber))
        //            query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));

        //        if (req.docsDate == true)
        //        {
        //            // Date range filter
        //            if (req.from.HasValue)
        //                query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value >= DateOnly.FromDateTime(req.from.Value));
        //            if (req.to.HasValue)
        //                query = query.Where(d => d.DocDate.HasValue && d.DocDate.Value <= DateOnly.FromDateTime(req.to.Value));
        //        }
        //        if(req.editDate == true)
        //        {
        //            // Edit date range filter
        //            if (req.from.HasValue)
        //                query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value >= req.from.Value);
        //            if (req.to.HasValue)
        //                query = query.Where(d => d.EditDate.HasValue && d.EditDate.Value <= req.to.Value);
        //        }   
        //        if(!string.IsNullOrWhiteSpace(req.systemId))
        //            query = query.Where(d => d.RefrenceNo != null && d.RefrenceNo.ToString().Contains(req.systemId));

        //        if (!string.IsNullOrWhiteSpace(req.subject))
        //            query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));

        //        if (!string.IsNullOrWhiteSpace(req.source))
        //            query = query.Where(d => d.DocSource != null && d.DocSource.ToString().Contains(req.source));

        //        if (!string.IsNullOrWhiteSpace(req.ReferenceTo))
        //            query = query.Where(d => d.ReferenceTo != null && d.ReferenceTo.Contains(req.ReferenceTo));

        //        if (!string.IsNullOrWhiteSpace(req.wordToSearch))
        //            query = query.Where(d => d.WordsTosearch != null && d.WordsTosearch.Contains(req.wordToSearch));

        //        if (!string.IsNullOrWhiteSpace(req.boxFile))
        //            query = query.Where(d => d.BoxfileNo != null && d.BoxfileNo.Contains(req.boxFile));

        //        if (!string.IsNullOrWhiteSpace(req.fileType))
        //            query = query.Where(d => d.FileType != null && d.FileType.ToString().Contains(req.fileType));



        //        // Paging
        //        var pagedQuery = query
        //            .OrderByDescending(d => d.Id)
        //            .Skip((req.pageNumber - 1) * req.pageSize)
        //            .Take(req.pageSize);

        //        var result = await pagedQuery.Select(d => new QuikSearchResponseDTOs
        //        {
        //            Id = d.Id,
        //            file = d.ImgUrl,
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

                // Custom filters not covered by BaseFilter
                if (!string.IsNullOrWhiteSpace(req.docsNumber))
                    query = query.Where(d => d.DocNo != null && d.DocNo.Contains(req.docsNumber));
                if (!string.IsNullOrWhiteSpace(req.subject))
                    query = query.Where(d => d.Subject != null && d.Subject.Contains(req.subject));
                // ... (repeat for other custom fields)

                // Paging and ordering
                var pagedQuery = query
                    .OrderByDescending(d => d.Id)
                    .Skip((req.pageNumber - 1) * req.pageSize)
                    .Take(req.pageSize);

                var result = await pagedQuery.Select(d => new QuikSearchResponseDTOs
                {
                    Id = d.Id,
                    file = d.ImgUrl,
                    docsNumber = d.DocNo,
                    docsDate = d.DocDate.HasValue ? d.DocDate.Value : null,
                    editDate = d.EditDate.HasValue ? d.EditDate.Value :null,
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


        // Detailed search method using IQueryable for better performance and flexibility
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
                    query = query.Where(d => d.RefrenceNo != null && d.RefrenceNo.Contains(req.systemId));
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
                    systemId = d.SystemId != null ? d.SystemId : null
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

        //this Implmentions for Search the DeletedDocs 
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

            var response = result.Select(doc => new BaseResponseDTOs(doc, 200, null)).ToList();

            return response;
        }
    }
}
