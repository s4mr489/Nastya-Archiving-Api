using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office.Word;
using iText.Forms.Fields.Merging;
using iText.Kernel.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Helper.Enums;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Reports;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.infrastructure;

namespace Nastya_Archiving_project.Services.reports
{
    public class ResportServices : BaseServices, IReportServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IInfrastructureServices _infrastructureServices;
        private readonly IArchivingSettingsServicers _archivingSettingsServicers;
        private readonly IEncryptionServices _encryptionServices;
        public ResportServices(IMapper mapper,
                                AppDbContext context,
                                IInfrastructureServices infrastructureServices,
                                IArchivingSettingsServicers archivingSettingsServicers,
                                IEncryptionServices encryptionServices) : base(mapper, context)
        {
            _mapper = mapper;
            _context = context;
            _infrastructureServices = infrastructureServices;
            _archivingSettingsServicers = archivingSettingsServicers;
            _encryptionServices = encryptionServices;
        }
        public async Task<BaseResponseDTOs> GetDepartmentEditorDocumentCountsPagedDetilesAsync(ReportsViewForm req)
        {
            if (req.resultType == EResultType.Detailed)
            {
                // 1. Get all departments
                var (departments, error) = await _infrastructureServices.GetAllDepartment();
                if (departments == null)
                {
                    return new BaseResponseDTOs(null, 500, error ?? "Failed to fetch departments");
                }

                // 2. Filter departments if departmentId filter is set
                var filteredDepartments = (req.departmentId != null && req.departmentId.Count > 0)
                    ? departments.Where(dept => req.departmentId.Contains(dept.Id)).ToList()
                    : departments;

                // 3. Get filtered documents using the same filter as GeneralReport
                var query = BuildFilteredQuery(req);

                int page = req.pageNumber > 0 ? req.pageNumber : 1;
                int pageSize = req.pageSize > 0 ? req.pageSize : 10;

                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // 4. Get paged documents
                var pagedDocs = await PaginateQuery(query, page, pageSize);

                // 5. Group paged documents by department and editor
                var grouped = filteredDepartments.Select(dept =>
                {
                    var editorGroups = pagedDocs
                        .Where(d => d.DepartId == dept.Id)
                        .GroupBy(d => d.Editor)
                        .Select(g => new
                        {
                            Editor = g.Key,
                            Documents = pagedDocs.Where(d => d.DepartId == dept.Id).Select(d => new
                            {
                                d.DocDate,
                                d.EditDate,
                                d.DocNo,
                                d.Subject,
                            }),
                            DocumentCount = g.Count()
                        }).ToList();

                    int departmentTotal = editorGroups.Sum(e => e.DocumentCount);

                    return new
                    {
                        DepartmentName = dept.DepartmentName,
                        DepartmentId = dept.Id,
                        Editors = editorGroups,
                        DepartmentTotal = departmentTotal
                    };
                }).ToList();

                int totalForAllDepartments = grouped.Sum(r => r.DepartmentTotal);

                var response = new
                {
                    Departments = grouped,
                    TotalForAllDepartments = totalForAllDepartments,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "any thing");
        }

        public async Task<BaseResponseDTOs> GetDepartmentEditorDocumentCountsPagedAsync(ReportsViewForm req)
        {
            // 1. Get all departments
            var (departments, error) = await _infrastructureServices.GetAllDepartment();
            if (departments == null)
            {
                return new BaseResponseDTOs(null, 500, error ?? "Failed to fetch departments");
            }

            // 2. Filter departments if departmentId filter is set
            var filteredDepartments = (req.departmentId != null && req.departmentId.Count > 0)
                ? departments.Where(dept => req.departmentId.Contains(dept.Id)).ToList()
                : departments;

            // 3. Get filtered documents using the same filter as GeneralReport
            var query = BuildFilteredQuery(req);

            int page = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 10;

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // 4. Get paged documents
            var pagedDocs = await PaginateQuery(query, page, pageSize);

            // 5. Group paged documents by department and editor
            var grouped = filteredDepartments.Select(dept =>
            {
                var editorGroups = pagedDocs
                    .Where(d => d.DepartId == dept.Id)
                    .GroupBy(d => d.Editor)
                    .Select(g => new
                    {
                        Editor = g.Key,
                        Documents = pagedDocs.Where(d => d.DepartId == dept.Id).Select(d => new
                        {
                            d.DocDate,
                            d.EditDate,
                            d.DocNo,
                            d.Subject,
                        }),
                        DocumentCount = g.Count()
                    }).ToList();

                int departmentTotal = editorGroups.Sum(e => e.DocumentCount);

                return new
                {
                    DepartmentName = dept.DepartmentName,
                    DepartmentId = dept.Id,
                    Editors = editorGroups,
                    DepartmentTotal = departmentTotal
                };
            }).ToList();

            int totalForAllDepartments = grouped.Sum(r => r.DepartmentTotal);

            var response = new
            {
                Departments = grouped,
                TotalForAllDepartments = totalForAllDepartments,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = page,
                PageSize = pageSize
            };

            return new BaseResponseDTOs(response, 200, null);
        }


        //that implmention to get the report based on the Department
        public async Task<BaseResponseDTOs> GetDepartmentDocumentsWithDetailsAsync(ReportsViewForm req)
        {
            // 1. Get all departments
            var (departments, error) = await _infrastructureServices.GetAllDepartment();
            if (departments == null)
            {
                return new BaseResponseDTOs(null, 500, error ?? "Failed to fetch departments");
            }

            // 2. Filter departments if departmentId filter is set
            var filteredDepartments = (req.departmentId != null && req.departmentId.Count > 0)
                ? departments.Where(dept => req.departmentId.Contains(dept.Id)).ToList()
                : departments;

            // 3. Get filtered documents using the same filter as GeneralReport
            var query = BuildFilteredQuery(req);

            int page = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 10;

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedDocs = await PaginateQuery(query, page, pageSize);

            // 4. Group paged documents by department, include departments with no docs
            var grouped = filteredDepartments.Select(dept => new
            {
                DepartmentName = dept.DepartmentName,
                DepartmentId = dept.Id,
                Documents = pagedDocs.Where(d => d.DepartId == dept.Id).Select(d => new
                {
                    d.DocDate,
                    d.EditDate,
                    d.DocNo,
                    d.Subject,
                })
            }).ToList();

            var response = new
            {
                Data = grouped,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = page,
                PageSize = pageSize
            };

            return new BaseResponseDTOs(response, 200, null);
        }

       

        public async Task<BaseResponseDTOs> GetDepartmentDocumentCountsAsync(ReportsViewForm req)
        {
            // 1. Get all departments
            var (departments, error) = await _infrastructureServices.GetAllDepartment();
            if (departments == null)
            {
                return new BaseResponseDTOs(null, 500, error ?? "Failed to fetch departments");
            }

            // 2. Filter departments if departmentId filter is set
            var filteredDepartments = (req.departmentId != null && req.departmentId.Count > 0)
                ? departments.Where(dept => req.departmentId.Contains(dept.Id)).ToList()
                : departments;

            // 3. Get filtered documents using the same filter as GeneralReport
            var query = BuildFilteredQuery(req);

            // 4. Group filtered documents by DepartId and count
            var docCounts = await query
                .GroupBy(d => d.DepartId)
                .Select(g => new { DepartId = g.Key, Count = g.Count() })
                .ToListAsync();

            // 5. Build a dictionary for fast lookup
            var docCountDict = docCounts
                .Where(x => x.DepartId.HasValue)
                .ToDictionary(x => x.DepartId.Value, x => x.Count);

            // 6. Build the result: for each department, get the count or 0
            var result = filteredDepartments.Select(dept => new
            {
                DepartmentName = dept.DepartmentName,
                DepartmentId = dept.Id,
                DocumentCount = docCountDict.TryGetValue(dept.Id, out var count) ? count : 0
            }).ToList();

            return new BaseResponseDTOs(result, 200, null);
        }

        //this implementation of the GeneralReport method provides a comprehensive report generation service that filters, paginates, enriches, and groups documents based on various criteria specified in the ReportsViewForm request.
        public async Task<BaseResponseDTOs> GeneralReport(ReportsViewForm req)
        {
            var query = BuildFilteredQuery(req);

            int page = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 10;

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalCount == 0)
            {
                return new BaseResponseDTOs(new
                {
                    Data = new List<object>(),
                    TotalCount = 0,
                    TotalPages = 0,
                    PageNumber = page,
                    PageSize = pageSize
                }, 200, null);
            }

            var docs = await PaginateQuery(query, page, pageSize);

            var enrichedDocs = await EnrichDocumentsAsync(docs);

            var grouped = GroupByDepartment(enrichedDocs);

            var response = new
            {
                Data = grouped,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = page,
                PageSize = pageSize
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        // Filtering
        private IQueryable<ArcivingDoc> BuildFilteredQuery(ReportsViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            if (req.fromEditingDate != null && req.toEditingDate != null)
                query = query.Where(x => x.EditDate >= req.fromEditingDate && x.EditDate <= req.toEditingDate);

            if (req.fromArchivingDate != null && req.toArchivingDate != null)
                query = query.Where(x => x.DocDate >= req.fromArchivingDate && x.DocDate <= req.toArchivingDate);

            if (req.docTypeId != null && req.docTypeId > 0)
                query = query.Where(x => x.DocType == req.docTypeId);

            if (req.sourceId != null && req.sourceId > 0)
                query = query.Where(x => x.DocSource == req.sourceId);

            if (req.toId != null && req.toId > 0)
                query = query.Where(x => x.DocTarget == req.toId);

            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => req.departmentId.Contains(x.DepartId));

            return query;
        }

        // Pagination
        private async Task<List<ArcivingDoc>> PaginateQuery(IQueryable<ArcivingDoc> query, int page, int pageSize)
        {
            return await query
                .OrderBy(x => x.DepartId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Enrichment
        private async Task<List<dynamic>> EnrichDocumentsAsync(List<ArcivingDoc> docs)
        {
            var enrichedDocs = new List<dynamic>();
            foreach (var doc in docs)
            {
                var (docTpyeObj, _) = doc.DocType > 0
                    ? await _archivingSettingsServicers.GetDocsTypeById(doc.DocType)
                    : (null, null);

                var (docTargeObj, _) = doc.DocTarget.HasValue && doc.DocTarget.Value > 0
                    ? await _infrastructureServices.GetPOrganizationById(doc.DocTarget.Value)
                    : (null, null);

                var (docSourceObj, _) = doc.DocSource.HasValue && doc.DocSource.Value > 0
                    ? await _infrastructureServices.GetPOrganizationById(doc.DocSource.Value)
                    : (null, null);

                var (supDocTypeObj, _) = doc.SubDocType.HasValue && doc.SubDocType.Value > 0
                    ? await _archivingSettingsServicers.GetDocsTypeById(doc.SubDocType.Value)
                    : (null, null);

                var (departObj, _) = doc.DepartId.HasValue && doc.DepartId.Value > 0
                    ? await _infrastructureServices.GetDepartmentById(doc.DepartId.Value)
                    : (null, null);

                enrichedDocs.Add(new
                {
                    doc.Id,
                    doc.RefrenceNo,
                    doc.DocType,
                    doc.Subject,
                    doc.DocSize,
                    doc.FileType,
                    doc.Editor,
                    doc.DocDate,
                    doc.EditDate,
                    doc.DepartId,
                    DepartmentName = departObj?.DepartmentName,
                    docSource = docSourceObj?.Dscrp,
                    docTarge = docTargeObj?.Dscrp,
                    docTpye = docTpyeObj?.docuName,
                    supDocType = supDocTypeObj?.docuName,
                });
            }
            return enrichedDocs;
        }

        // Grouping
        private List<object> GroupByDepartment(List<dynamic> enrichedDocs)
        {
            return enrichedDocs
                .GroupBy(d => d.DepartId)
                .Select(g => new
                {
                    DepartId = g.Key,
                    DepartmentName = g.FirstOrDefault()?.DepartmentName,
                    Documents = g.ToList()
                })
                .ToList<object>();
        }
    }
    
}
