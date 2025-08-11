using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office.PowerPoint.Y2021.M06.Main;
using DocumentFormat.OpenXml.Office.Word;
using iText.Forms.Fields.Merging;
using iText.Kernel.Crypto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OpenApi.Writers;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Helper.Enums;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Organization;
using Nastya_Archiving_project.Models.DTOs.Reports;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;
using Org.BouncyCastle.Asn1.X509;
using System.Drawing;

namespace Nastya_Archiving_project.Services.reports
{
    public class ResportServices : BaseServices, IReportServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IInfrastructureServices _infrastructureServices;
        private readonly IArchivingSettingsServicers _archivingSettingsServicers;
        private readonly IEncryptionServices _encryptionServices;
        private readonly ReportGenerator _reportGenerator;
        private readonly ISystemInfoServices _systemInfoServices;
        public ResportServices(IMapper mapper,
                                AppDbContext context,
                                IInfrastructureServices infrastructureServices,
                                IArchivingSettingsServicers archivingSettingsServicers,
                                IEncryptionServices encryptionServices,
                                ISystemInfoServices systemInfoServices) : base(mapper, context)
        {
            _mapper = mapper;
            _context = context;
            _infrastructureServices = infrastructureServices;
            _archivingSettingsServicers = archivingSettingsServicers;
            _encryptionServices = encryptionServices;
            _systemInfoServices = systemInfoServices;
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

        //this is the etiles Report for the Users based on the deparment 
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

                // Sort departments to maintain consistent order
                filteredDepartments = filteredDepartments.OrderBy(d => d.Id).ToList();

                // 3. Get filtered documents using the same filter as GeneralReport
                var query = BuildFilteredQuery(req);
                
                // Get total document count
                int totalCount = await query.CountAsync();
                
                // 4. Find departments with documents
                var departmentsWithDocs = new List<int>();
                foreach (var dept in filteredDepartments)
                {
                    int count = await query.Where(d => d.DepartId == dept.Id).CountAsync();
                    if (count > 0)
                        departmentsWithDocs.Add(dept.Id);
                }
                
                if (departmentsWithDocs.Count == 0)
                {
                    return new BaseResponseDTOs(new
                    {
                        CurrentDepartment = new { 
                            DepartmentName = string.Empty,
                            DepartmentId = 0,
                            Editors = new List<object>(),
                            DepartmentTotal = 0
                        },
                        TotalDepartments = 0,
                        CurrentDepartmentIndex = 0,
                        TotalCount = 0,
                        TotalPages = 0,
                        PageNumber = req.pageNumber,
                        PageSize = req.pageSize,
                        TotalDocumentCount = 0
                    }, 200, null);
                }
                
                // 5. Handle department-based pagination
                int deptPage = req.pageNumber > 0 ? req.pageNumber : 1;
                int deptPageSize = 1; // Show 1 department per page
                
                // Calculate which department to show based on page number
                int departmentIndex = (deptPage - 1) % departmentsWithDocs.Count;
                int currentDepartmentId = departmentsWithDocs[departmentIndex];
                var currentDepartment = filteredDepartments.First(d => d.Id == currentDepartmentId);
                
                // 6. Now handle document pagination within the department
                int docPage = req.docPage > 0 ? req.docPage : 1;
                int docPageSize = req.pageSize > 0 ? req.pageSize : 10;
                
                // Get all documents for this department
                var departmentDocsQuery = query.Where(d => d.DepartId == currentDepartmentId);
                int departmentTotalDocs = await departmentDocsQuery.CountAsync();
                int departmentTotalPages = (int)Math.Ceiling(departmentTotalDocs / (double)docPageSize);
                
                // Get paged documents for this department
                var departmentDocs = await departmentDocsQuery
                    .OrderBy(d => d.DocNo)
                    .Skip((docPage - 1) * docPageSize)
                    .Take(docPageSize)
                    .ToListAsync();
                
                // 7. Group department documents by editor
                var editorGroups = departmentDocs
                    .GroupBy(d => d.Editor)
                    .Select(g => new
                    {
                        Editor = g.Key,
                        Documents = g.Select(d => new
                        {
                            d.DocDate,
                            d.EditDate,
                            d.DocNo,
                            d.Subject,
                        }).ToList(),
                        DocumentCount = g.Count()
                    })
                    .OrderBy(g => g.Editor)
                    .ToList();

                // 8. Create response with single department but with document pagination info
                var departmentData = new
                {
                    DepartmentName = currentDepartment.DepartmentName,
                    DepartmentId = currentDepartment.Id,
                    Editors = editorGroups,
                    DepartmentTotal = departmentTotalDocs,
                    CurrentDocPage = docPage,
                    TotalDocPages = departmentTotalPages
                };
                
                // Department pagination is based on total departments with documents
                int totalDepartmentPages = departmentsWithDocs.Count;

                var response = new
                {
                    CurrentDepartment = departmentData,
                    TotalDepartments = departmentsWithDocs.Count,
                    CurrentDepartmentIndex = departmentIndex + 1, // 1-based index for display
                    TotalDepartmentPages = totalDepartmentPages,
                    CurrentDepartmentPage = deptPage,
                    TotalDocumentCount = totalCount, // Total across all departments
                    FilteredDepartmentIds = departmentsWithDocs // For debugging/reference
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "Please use Detailed result type for this report");
        }

        //this is the sticitcs Report for the Users based on the deparment 
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

            // 5. Group paged documents by department and editor, only return department, editor, editor count, and department total
            var grouped = filteredDepartments.Select(dept =>
            {
                var editorGroups = pagedDocs
                    .Where(d => d.DepartId == dept.Id)
                    .GroupBy(d => d.Editor)
                    .Select(g => new
                    {
                        Editor = g.Key,
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

            // Sort departments to maintain consistent order
            filteredDepartments = filteredDepartments.OrderBy(d => d.Id).ToList();

            // 3. Get filtered documents using the same filter as GeneralReport
            var query = BuildFilteredQuery(req);
            
            // Get total document count for pagination info
            int totalCount = await query.CountAsync();
            
            // 4. Handle department-based pagination
            int deptPage = req.pageNumber > 0 ? req.pageNumber : 1;
            int deptPageSize = 1; // Show 1 department per page
            
            // Find departments with documents
            var departmentsWithDocs = new List<int>();
            foreach (var dept in filteredDepartments)
            {
                int count = await query.Where(d => d.DepartId == dept.Id).CountAsync();
                if (count > 0)
                    departmentsWithDocs.Add(dept.Id);
            }
            
            if (departmentsWithDocs.Count == 0)
            {
                return new BaseResponseDTOs(new
                {
                    CurrentDepartment = new {
                        DepartmentName = string.Empty,
                        DepartmentId = 0,
                        Documents = new List<object>(),
                        TotalDocuments = 0,
                        CurrentDocPage = 0,
                        TotalDocPages = 0
                    },
                    TotalDepartments = 0,
                    CurrentDepartmentIndex = 0,
                    TotalDepartmentPages = 0,
                    CurrentDepartmentPage = deptPage,
                    TotalDocumentCount = 0
                }, 200, null);
            }
            
            // Calculate current department index based on page number
            int departmentIndex = (deptPage - 1) % departmentsWithDocs.Count;
            int currentDepartmentId = departmentsWithDocs[departmentIndex];
            var currentDepartment = filteredDepartments.First(d => d.Id == currentDepartmentId);
            
            // 5. Handle document pagination within the department
            int docPage = req.docPage > 0 ? req.docPage : 1;
            int docPageSize = req.pageSize > 0 ? req.pageSize : 10;
            
            // Get total documents for this department to calculate document pagination
            var departmentDocsQuery = query.Where(d => d.DepartId == currentDepartmentId);
            int departmentTotalDocs = await departmentDocsQuery.CountAsync();
            int departmentTotalPages = (int)Math.Ceiling(departmentTotalDocs / (double)docPageSize);
            
            // Get paged documents for this department
            var departmentDocs = await departmentDocsQuery
                .OrderBy(d => d.DocDate)
                .Skip((docPage - 1) * docPageSize)
                .Take(docPageSize)
                .ToListAsync();
            
            // Format documents for display
            var documents = departmentDocs.Select(d => new
            {
                docDate = d.DocDate,
                editDate = d.EditDate,
                docNo = d.DocNo,
                subject = d.Subject,
            }).ToList();
            
            // 6. Create response with single department and document pagination info
            var response = new
            {
                CurrentDepartment = new
                {
                    DepartmentName = currentDepartment.DepartmentName,
                    DepartmentId = currentDepartment.Id,
                    Documents = documents,
                    TotalDocuments = departmentTotalDocs,
                    CurrentDocPage = docPage,
                    TotalDocPages = departmentTotalPages
                },
                TotalDepartments = departmentsWithDocs.Count,
                CurrentDepartmentIndex = departmentIndex + 1, // 1-based index for display
                TotalDepartmentPages = departmentsWithDocs.Count,
                CurrentDepartmentPage = deptPage,
                TotalDocumentCount = totalCount // Total across all departments
            };

            // Return response
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

        //that implementation provides a statistical report of monthly documents for each department, including document counts grouped by month and department, with pagination and filtering capabilities.

        public async Task<BaseResponseDTOs> GetDepartmentMonthlyDocumentCountsPagedAsync(ReportsViewForm req)
        {
            if (req.resultType == EResultType.statistical)
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

                // 5. Group paged documents by department and month
                var grouped = filteredDepartments.Select(dept =>
                {
                    var monthGroups = pagedDocs
                        .Where(d => d.DepartId == dept.Id && d.DocDate.HasValue)
                        .GroupBy(d => new { d.EditDate.Value.Year, d.EditDate.Value.Month })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            DocumentCount = g.Count()
                        })
                        .OrderBy(g => g.Year).ThenBy(g => g.Month)
                        .ToList();

                    int departmentTotal = monthGroups.Sum(m => m.DocumentCount);

                    return new
                    {
                        DepartmentName = dept.DepartmentName,
                        DepartmentId = dept.Id,
                        Months = monthGroups,
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
                    PageSize = pageSize,
                    TotalDocumentCount = pagedDocs.Count // Add total document count for this page
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "That is statistacal Report");
        }

        //that implementation provides a detailed report of monthly documents for each department, including document counts grouped by month and department, with pagination and filtering capabilities.
        public async Task<BaseResponseDTOs> GetDepartmentMonthlyDocumentDetailsPagedAsync(ReportsViewForm req)
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

                // Sort departments to maintain consistent order
                filteredDepartments = filteredDepartments.OrderBy(d => d.Id).ToList();

                // 3. Get filtered documents using the same filter as GeneralReport
                var query = BuildFilteredQuery(req);
                
                // Get total document count for pagination info
                int totalCount = await query.CountAsync();
                
                // 4. Handle department-based pagination
                int deptPage = req.pageNumber > 0 ? req.pageNumber : 1;
                int deptPageSize = 1; // Show 1 department per page
                
                // Find departments with documents
                var departmentsWithDocs = new List<int>();
                foreach (var dept in filteredDepartments)
                {
                    int count = await query.Where(d => d.DepartId == dept.Id).CountAsync();
                    if (count > 0)
                        departmentsWithDocs.Add(dept.Id);
                }
                
                if (departmentsWithDocs.Count == 0)
                {
                    return new BaseResponseDTOs(new
                    {
                        CurrentDepartment = new {
                            DepartmentName = string.Empty,
                            DepartmentId = 0,
                            Months = new List<object>(),
                            DepartmentTotal = 0
                        },
                        TotalDepartments = 0,
                        CurrentDepartmentIndex = 0,
                        TotalCount = 0,
                        TotalPages = 0,
                        PageNumber = deptPage,
                        PageSize = req.pageSize,
                        TotalDocumentCount = 0
                    }, 200, null);
                }
                
                // Calculate current department index based on page number
                int departmentIndex = (deptPage - 1) % departmentsWithDocs.Count;
                int currentDepartmentId = departmentsWithDocs[departmentIndex];
                var currentDepartment = filteredDepartments.First(d => d.Id == currentDepartmentId);
                
                // 5. Handle document pagination within the department
                int docPage = req.docPage > 0 ? req.docPage : 1;
                int docPageSize = req.pageSize > 0 ? req.pageSize : 10;
                
                // Get total documents for this department to calculate document pagination
                var departmentDocsQuery = query.Where(d => d.DepartId == currentDepartmentId);
                int departmentTotalDocs = await departmentDocsQuery.CountAsync();
                int departmentTotalPages = (int)Math.Ceiling(departmentTotalDocs / (double)docPageSize);
                
                // Get paged documents for this department
                var departmentDocs = await departmentDocsQuery
                    .OrderBy(d => d.DocDate)
                    .Skip((docPage - 1) * docPageSize)
                    .Take(docPageSize)
                    .ToListAsync();
                
                // 6. Group department documents by month
                var monthGroups = departmentDocs
                    .Where(d => d.DocDate.HasValue && d.EditDate.HasValue)
                    .GroupBy(d => new { d.EditDate.Value.Year, d.EditDate.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Documents = g.Select(d => new
                        {
                            d.DocDate,
                            d.EditDate,
                            d.DocNo,
                            d.Subject,
                        }).ToList(),
                        DocumentCount = g.Count()
                    })
                    .OrderBy(g => g.Year).ThenBy(g => g.Month)
                    .ToList();

                // 7. Create response with single department but with document pagination info
                var departmentData = new
                {
                    DepartmentName = currentDepartment.DepartmentName,
                    DepartmentId = currentDepartment.Id,
                    Months = monthGroups,
                    DepartmentTotal = departmentTotalDocs,
                    CurrentDocPage = docPage,
                    TotalDocPages = departmentTotalPages
                };
                
                // Department pagination is based on total departments with documents
                int totalDepartmentPages = departmentsWithDocs.Count;

                var response = new
                {
                    CurrentDepartment = departmentData,
                    TotalDepartments = departmentsWithDocs.Count,
                    CurrentDepartmentIndex = departmentIndex + 1, // 1-based index for display
                    TotalDepartmentPages = totalDepartmentPages,
                    CurrentDepartmentPage = deptPage,
                    TotalDocumentCount = totalCount // Total across all departments
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "Please use Detailed result type for this report");
        }

        public async Task<BaseResponseDTOs> GetSourceMonthlyDocumentCountsPagedAsync(ReportsViewForm req)
        {
            if (req.resultType == EResultType.statistical)
            {
                // 1. Get all sources (distinct DocSource values from filtered docs)
                var query = BuildFilteredQuery(req);

                int page = req.pageNumber > 0 ? req.pageNumber : 1;
                int pageSize = req.pageSize > 0 ? req.pageSize : 10;

                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // 2. Get paged documents
                var pagedDocs = await PaginateQuery(query, page, pageSize);

                // 3. Get all sources for the paged docs
                var sourceIds = pagedDocs
                    .Where(d => d.DocSource.HasValue)
                    .Select(d => d.DocSource.Value)
                    .Distinct()
                    .ToList();

                // 4. Optionally, get source names from infrastructure service
                var sources = await _infrastructureServices.GetAllPOrganizations();
                var filteredSources = sources.POrganization?.Where(s => sourceIds.Contains(s.Id)).ToList() ?? new List<OrgniztionResponseDTOs>();
                // 5. Group paged documents by source and month
                var grouped = filteredSources.Select(src =>
                {
                    var monthGroups = pagedDocs
                        .Where(d => d.DocSource == src.Id && d.DocDate.HasValue)
                        .GroupBy(d => new { d.DocDate.Value.Year, d.DocDate.Value.Month })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            DocumentCount = g.Count()
                        })
                        .OrderBy(g => g.Year).ThenBy(g => g.Month)
                        .ToList();

                    int sourceTotal = monthGroups.Sum(m => m.DocumentCount);

                    return new
                    {
                        SourceName = src.Dscrp,
                        SourceId = src.Id,
                        Months = monthGroups,
                        SourceTotal = sourceTotal
                    };
                }).ToList();

                int totalForAllSources = grouped.Sum(r => r.SourceTotal);

                var response = new
                {
                    Sources = grouped,
                    TotalForAllSources = totalForAllSources,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalDocumentCount = pagedDocs.Count
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "That is statistical Report");
        }


        public async Task<BaseResponseDTOs> GetSourceMonthlyDocumentDetailsPagedAsync(ReportsViewForm req)
        {
            if (req.resultType == EResultType.Detailed)
            {
                // 1. Get all sources (distinct DocSource values from filtered docs)
                var query = BuildFilteredQuery(req);

                // Get total document count for pagination info
                int totalCount = await query.CountAsync();
                
                // 2. Find all unique sources with documents
                var sourceIds = await query
                    .Where(d => d.DocSource.HasValue)
                    .Select(d => d.DocSource.Value)
                    .Distinct()
                    .ToListAsync();
                
                // 3. Get source organizations
                var sources = await _infrastructureServices.GetAllPOrganizations();
                var filteredSources = sources.POrganization?
                    .Where(s => sourceIds.Contains(s.Id))
                    .OrderBy(s => s.Id)
                    .ToList() ?? new List<OrgniztionResponseDTOs>();
                
                if (filteredSources.Count == 0)
                {
                    return new BaseResponseDTOs(new
                    {
                        CurrentSource = new { 
                            SourceName = string.Empty,
                            SourceId = 0,
                            Months = new List<object>(),
                            SourceTotal = 0,
                            CurrentDocPage = 0,
                            TotalDocPages = 0
                        },
                        TotalSources = 0,
                        CurrentSourceIndex = 0,
                        TotalSourcePages = 0,
                        CurrentSourcePage = 0,
                        TotalCount = 0,
                        TotalDocumentCount = 0
                    }, 200, null);
                }
                
                // 4. Handle source-based pagination
                int sourcePage = req.pageNumber > 0 ? req.pageNumber : 1;
                
                // Calculate current source index based on page number
                int sourceIndex = (sourcePage - 1) % filteredSources.Count;
                var currentSource = filteredSources[sourceIndex];
                
                // 5. Handle document pagination within the source
                int docPage = req.docPage > 0 ? req.docPage : 1;
                int docPageSize = req.pageSize > 0 ? req.pageSize : 10;
                
                // Get total documents for this source
                var sourceDocsQuery = query.Where(d => d.DocSource == currentSource.Id && d.DocDate.HasValue);
                int sourceTotalDocs = await sourceDocsQuery.CountAsync();
                int sourceTotalPages = (int)Math.Ceiling(sourceTotalDocs / (double)docPageSize);
                
                // Get paged documents for this source
                var sourceDocs = await sourceDocsQuery
                    .OrderBy(d => d.DocDate)
                    .Skip((docPage - 1) * docPageSize)
                    .Take(docPageSize)
                    .ToListAsync();
                
                // 6. Group source documents by month
                var monthGroups = sourceDocs
                    .GroupBy(d => new { d.DocDate.Value.Year, d.DocDate.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Documents = g.Select(d => new
                        {
                            d.DocDate,
                            d.EditDate,
                            d.DocNo,
                            d.Subject,
                        }).ToList(),
                        DocumentCount = g.Count()
                    })
                    .OrderBy(g => g.Year).ThenBy(g => g.Month)
                    .ToList();
        
                // 7. Create response with single source and document pagination info
                var sourceData = new
                {
                    SourceName = currentSource.Dscrp,
                    SourceId = currentSource.Id,
                    Months = monthGroups,
                    SourceTotal = sourceTotalDocs,
                    CurrentDocPage = docPage,
                    TotalDocPages = sourceTotalPages
                };
                
                var response = new
                {
                    CurrentSource = sourceData,
                    TotalSources = filteredSources.Count,
                    CurrentSourceIndex = sourceIndex + 1, // 1-based index for display
                    TotalSourcePages = filteredSources.Count,
                    CurrentSourcePage = sourcePage,
                    TotalCount = totalCount,
                    TotalDocumentCount = totalCount
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "Please use Detailed result type for this report");
        }

        public async Task<BaseResponseDTOs> GetTargeteMonthlyDocumentCountsPagedAsync(ReportsViewForm req)
        {
            if (req.resultType == EResultType.statistical)
            {
                // 1. Get all sources (distinct DocSource values from filtered docs)
                var query = BuildFilteredQuery(req);

                int page = req.pageNumber > 0 ? req.pageNumber : 1;
                int pageSize = req.pageSize > 0 ? req.pageSize : 10;

                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // 2. Get paged documents
                var pagedDocs = await PaginateQuery(query, page, pageSize);

                // 3. Get all sources for the paged docs
                var targetId = pagedDocs
                    .Where(d => d.DocTarget.HasValue)
                    .Select(d => d.DocTarget.Value)
                    .Distinct()
                    .ToList();

                // 4. Optionally, get source names from infrastructure service
                var sources = await _infrastructureServices.GetAllPOrganizations();
                var filteredSources = sources.POrganization?.Where(s => targetId.Contains(s.Id)).ToList() ?? new List<OrgniztionResponseDTOs>();
                // 5. Group paged documents by source and month
                var grouped = filteredSources.Select(src =>
                {
                    var monthGroups = pagedDocs
                        .Where(d => d.DocTarget == src.Id && d.DocTarget.HasValue)
                        .GroupBy(d => new { d.DocDate.Value.Year, d.DocDate.Value.Month })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            DocumentCount = g.Count()
                        })
                        .OrderBy(g => g.Year).ThenBy(g => g.Month)
                        .ToList();

                    int sourceTotal = monthGroups.Sum(m => m.DocumentCount);

                    return new
                    {
                        TargetName = src.Dscrp,
                        TargetId = src.Id,
                        Months = monthGroups,
                        TargetTotal = sourceTotal
                    };
                }).ToList();

                int totalForAllSources = grouped.Sum(r => r.TargetTotal);

                var response = new
                {
                    Target = grouped,
                    TotalForAllSources = totalForAllSources,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalDocumentCount = pagedDocs.Count
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "That is statistical Report");
        }

        public async Task<BaseResponseDTOs> GetReferncesDocsDetailsPagedAsync(ReportsViewForm req)
        {
            if (req.resultType == EResultType.Detailed)
            {
                // Apply filters to parent docs
                var filteredParentDocs = BuildFilteredQuery(req);
                
                // Query to join reference documents
                var joinQuery = 
                    from parent in filteredParentDocs
                    join reference in _context.ArcivDocsRefrences on parent.RefrenceNo equals reference.HeadReferenceNo
                    join joined in _context.ArcivingDocs on reference.LinkedRfrenceNo equals joined.RefrenceNo
                    join dept in _context.GpDepartments on joined.DepartId equals dept.Id
                    join org in _context.POrganizations on joined.DocSource equals org.Id into orgJoin
                    from org in orgJoin.DefaultIfEmpty()
                    join docType in _context.ArcivDocDscrps on joined.DocType equals docType.Id into docTypeJoin
                    from docType in docTypeJoin.DefaultIfEmpty()
                    select new
                    {
                        DepartmentId = joined.DepartId,
                        DepartDscrp = dept.Dscrp,
                        ParentDoc = parent.RefrenceNo,
                        JoinedDoc = joined.RefrenceNo,
                        joined.DocNo,
                        joined.DocDate,
                        Organization = org != null ? org.Dscrp : null,
                        DocType = docType != null ? docType.Dscrp : null,
                        joined.Subject,
                        joined.BoxfileNo
                    };
        
                // Get total count
                int totalCount = await joinQuery.CountAsync();
                
                // Get distinct departments with reference documents
                var departmentIds = await joinQuery
                    .Select(x => x.DepartmentId)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .Distinct()
                    .ToListAsync();
        
                if (departmentIds.Count == 0)
                {
                    return new BaseResponseDTOs(new
                    {
                        CurrentDepartment = new { 
                            DepartmentName = string.Empty,
                            DepartmentId = 0,
                            Documents = new List<object>(),
                            TotalDocuments = 0,
                            CurrentDocPage = 0,
                            TotalDocPages = 0
                        },
                        TotalDepartments = 0,
                        CurrentDepartmentIndex = 0,
                        TotalDepartmentPages = 0,
                        CurrentDepartmentPage = 0,
                        TotalDocumentCount = 0
                    }, 200, null);
                }
        
                // Get department info
                var (departments, error) = await _infrastructureServices.GetAllDepartment();
                if (departments == null)
                {
                    return new BaseResponseDTOs(null, 500, error ?? "Failed to fetch departments");
                }
        
                var departmentsWithDocs = departments
                    .Where(d => departmentIds.Contains(d.Id))
                    .OrderBy(d => d.Id)
                    .ToList();
        
                // Handle department-based pagination
                int deptPage = req.pageNumber > 0 ? req.pageNumber : 1;
                
                // Calculate current department index based on page number
                int departmentIndex = (deptPage - 1) % departmentsWithDocs.Count;
                var currentDepartment = departmentsWithDocs[departmentIndex];
        
                // Handle document pagination within the department
                int docPage = req.docPage > 0 ? req.docPage : 1;
                int docPageSize = req.pageSize > 0 ? req.pageSize : 10;
        
                // Get total documents for this department
                var departmentDocsQuery = joinQuery.Where(x => x.DepartmentId == currentDepartment.Id);
                int departmentTotalDocs = await departmentDocsQuery.CountAsync();
                int departmentTotalPages = (int)Math.Ceiling(departmentTotalDocs / (double)docPageSize);
        
                // Get paged documents for current department
                var departmentDocs = await departmentDocsQuery
                    .OrderByDescending(x => x.JoinedDoc)
                    .Skip((docPage - 1) * docPageSize)
                    .Take(docPageSize)
                    .ToListAsync();
        
                var response = new
                {
                    CurrentDepartment = new
                    {
                        DepartmentName = currentDepartment.DepartmentName,
                        DepartmentId = currentDepartment.Id,
                        Documents = departmentDocs,
                        TotalDocuments = departmentTotalDocs,
                        CurrentDocPage = docPage,
                        TotalDocPages = departmentTotalPages
                    },
                    TotalDepartments = departmentsWithDocs.Count,
                    CurrentDepartmentIndex = departmentIndex + 1,
                    TotalDepartmentPages = departmentsWithDocs.Count,
                    CurrentDepartmentPage = deptPage,
                    TotalDocumentCount = totalCount
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "Invalid result type");
        }

        public async Task<BaseResponseDTOs> GetTargetMonthlyDocumentDetailsPagedAsync(ReportsViewForm req)
        {
            if (req.resultType == EResultType.Detailed)
            {
                // 1. Get all sources (distinct DocSource values from filtered docs)
                var query = BuildFilteredQuery(req);

                // Get total document count for pagination info
                int totalCount = await query.CountAsync();
                
                // 2. Find all unique targets with documents
                var targetIds = await query
                    .Where(d => d.DocTarget.HasValue)
                    .Select(d => d.DocTarget.Value)
                    .Distinct()
                    .ToListAsync();
                
                // 3. Get target organizations
                var sources = await _infrastructureServices.GetAllPOrganizations();
                var filteredTargets = sources.POrganization?
                    .Where(s => targetIds.Contains(s.Id))
                    .OrderBy(s => s.Id)
                    .ToList() ?? new List<OrgniztionResponseDTOs>();
                
                if (filteredTargets.Count == 0)
                {
                    return new BaseResponseDTOs(new
                    {
                        CurrentTarget = new { 
                            TargetName = string.Empty,
                            TargetId = 0,
                            Months = new List<object>(),
                            TargetTotal = 0,
                            CurrentDocPage = 0,
                            TotalDocPages = 0
                        },
                        TotalTargets = 0,
                        CurrentTargetIndex = 0,
                        TotalTargetPages = 0,
                        CurrentTargetPage = 0,
                        TotalCount = 0,
                        TotalDocumentCount = 0
                    }, 200, null);
                }
                
                // 4. Handle target-based pagination
                int targetPage = req.pageNumber > 0 ? req.pageNumber : 1;
                
                // Calculate current target index based on page number
                int targetIndex = (targetPage - 1) % filteredTargets.Count;
                var currentTarget = filteredTargets[targetIndex];
                
                // 5. Handle document pagination within the target
                int docPage = req.docPage > 0 ? req.docPage : 1;
                int docPageSize = req.pageSize > 0 ? req.pageSize : 10;
                
                // Get total documents for this target
                var targetDocsQuery = query.Where(d => d.DocTarget == currentTarget.Id && d.DocDate.HasValue);
                int targetTotalDocs = await targetDocsQuery.CountAsync();
                int targetTotalPages = (int)Math.Ceiling(targetTotalDocs / (double)docPageSize);
                
                // Get paged documents for this target
                var targetDocs = await targetDocsQuery
                    .OrderBy(d => d.DocDate)
                    .Skip((docPage - 1) * docPageSize)
                    .Take(docPageSize)
                    .ToListAsync();
                
                // 6. Group target documents by month
                var monthGroups = targetDocs
                    .GroupBy(d => new { d.DocDate.Value.Year, d.DocDate.Value.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Documents = g.Select(d => new
                        {
                            d.DocDate,
                            d.EditDate,
                            d.DocNo,
                            d.Subject,
                        }).ToList(),
                        DocumentCount = g.Count()
                    })
                    .OrderBy(g => g.Year).ThenBy(g => g.Month)
                    .ToList();

                // 7. Create response with single target and document pagination info
                var targetData = new
                {
                    TargetName = currentTarget.Dscrp,
                    TargetId = currentTarget.Id,
                    Months = monthGroups,
                    TargetTotal = targetTotalDocs,
                    CurrentDocPage = docPage,
                    TotalDocPages = targetTotalPages
                };
                
                var response = new
                {
                    CurrentTarget = targetData,
                    TotalTargets = filteredTargets.Count,
                    CurrentTargetIndex = targetIndex + 1, // 1-based index for display
                    TotalTargetPages = filteredTargets.Count,
                    CurrentTargetPage = targetPage,
                    TotalCount = totalCount,
                    TotalDocumentCount = totalCount
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "Please use Detailed result type for this report");
        }

        public async Task<BaseResponseDTOs> CheckDocumentsFileIntegrityPagedAsync(int page, int pageSize)
        {
            // Ensure page is at least 1
            page = page < 1 ? 1 : page;
            int skip = (page - 1) * pageSize;
            var query = _context.ArcivingDocs.OrderBy(x => x.Id);

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var docs = await query.OrderByDescending(d => d.EditDate).Skip(skip).Take(pageSize).ToListAsync();

            var result = new List<object>();
            foreach (var doc in docs)
            {
                string filePath = doc.FileType != null ? doc.FileType.ToString() : null;
                decimal? expectedSize = doc.DocSize;
                long actualSize = -1;
                bool isAffected = true;

                if (!string.IsNullOrEmpty(filePath) && expectedSize != null && System.IO.File.Exists(filePath))
                {
                    actualSize = new System.IO.FileInfo(filePath).Length;
                    isAffected = actualSize != (long)expectedSize;
                }

                result.Add(new
                {
                    DocumentId = doc.Id,
                    FilePath = filePath,
                    ExpectedSize = expectedSize,
                    ActualSize = actualSize >= 0 ? (long?)actualSize : null,
                    IsAffected = isAffected
                });
            }

            return new BaseResponseDTOs(new
            {
                Data = result,
                TotalCount = totalCount,
                TotalPages = totalPages,
                PageNumber = page,
                PageSize = pageSize
            }, 200, null);
        }
        public async Task<BaseResponseDTOs> GetMontlyUsersDocumentDetailsPagedList(ReportsViewForm req)
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

                // Sort departments to maintain consistent order
                filteredDepartments = filteredDepartments.OrderBy(d => d.Id).ToList();

                // 3. Get filtered documents using the same filter as GeneralReport
                var query = BuildFilteredQuery(req);
                
                // Get total document count for pagination info
                int totalCount = await query.CountAsync();
                
                // 4. Find departments with documents
                var departmentsWithDocs = new List<int>();
                foreach (var dept in filteredDepartments)
                {
                    int count = await query.Where(d => d.DepartId == dept.Id).CountAsync();
                    if (count > 0)
                        departmentsWithDocs.Add(dept.Id);
                }
                
                if (departmentsWithDocs.Count == 0)
                {
                    return new BaseResponseDTOs(new
                    {
                        CurrentDepartment = new { 
                            DepartmentName = string.Empty,
                            DepartmentId = 0,
                            Editors = new List<object>(),
                            DepartmentTotal = 0
                        },
                        TotalDepartments = 0,
                        CurrentDepartmentIndex = 0,
                        TotalCount = 0,
                        TotalPages = 0,
                        PageNumber = req.pageNumber,
                        PageSize = req.pageSize,
                        TotalDocumentCount = 0
                    }, 200, null);
                }
                
                // 5. Handle department-based pagination
                int deptPage = req.pageNumber > 0 ? req.pageNumber : 1;
                int deptPageSize = 1; // Show 1 department per page
                
                // Calculate which department to show based on page number
                int departmentIndex = (deptPage - 1) % departmentsWithDocs.Count;
                int currentDepartmentId = departmentsWithDocs[departmentIndex];
                var currentDepartment = filteredDepartments.First(d => d.Id == currentDepartmentId);
                
                // 6. Now handle document pagination within the department
                int docPage = req.docPage > 0 ? req.docPage : 1;
                int docPageSize = req.pageSize > 0 ? req.pageSize : 10;
                
                // Get all documents for this department
                var departmentDocsQuery = query.Where(d => d.DepartId == currentDepartmentId);
                int departmentTotalDocs = await departmentDocsQuery.CountAsync();
                int departmentTotalPages = (int)Math.Ceiling(departmentTotalDocs / (double)docPageSize);
                
                // Get paged documents for this department
                var departmentDocs = await departmentDocsQuery
                    .OrderBy(d => d.Editor)
                    .Skip((docPage - 1) * docPageSize)
                    .Take(docPageSize)
                    .ToListAsync();
                
                // 7. Group department documents by editor
                var editorGroups = departmentDocs
                    .Where(d => d.DocDate.HasValue)
                    .GroupBy(d => new { Editor = d.Editor, Month = d.DocDate.Value.Month })
                    .Select(g => new
                    {
                        Editor = g.Key.Editor,
                        Month = g.Key.Month,
                        Documents = g.Select(d => new
                        {
                            d.DocDate,
                            d.EditDate,
                            d.DocNo,
                            d.Subject,
                        }).ToList(),
                        DocumentCount = g.Count()
                    })
                    .OrderBy(g => g.Editor).ThenBy(g => g.Month)
                    .ToList();

                int departmentTotal = departmentDocs.Count;
                
                // 8. Create response with single department
                var departmentData = new
                {
                    DepartmentName = currentDepartment.DepartmentName,
                    DepartmentId = currentDepartment.Id,
                    Editors = editorGroups,
                    DepartmentTotal = departmentTotal,
                    CurrentDocPage = docPage,
                    TotalDocPages = departmentTotalPages
                };
                
                // Department pagination is based on total departments with documents
                int totalDepartmentPages = departmentsWithDocs.Count;

                var response = new
                {
                    CurrentDepartment = departmentData,
                    TotalDepartments = departmentsWithDocs.Count,
                    CurrentDepartmentIndex = departmentIndex + 1, // 1-based index for display
                    TotalDepartmentPages = departmentsWithDocs.Count,
                    CurrentDepartmentPage = deptPage,
                    TotalDocumentCount = totalCount // Total across all departments
                };

                return new BaseResponseDTOs(response, 200, null);
            }
            return new BaseResponseDTOs(null, 400, "Please use Detailed result type for this report");
        }
        
        public async Task<BaseResponseDTOs> GetMonthlyUsersDocumentCountPagedAsync(ReportsViewForm req)
        {
            if (req.resultType == EResultType.statistical)
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
                        .GroupBy(d => new { d.Editor, d.DocDate.Value.Month })
                        .Select(g => new
                        {
                            Editor = g.Key,
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



        public async Task<BaseResponseDTOs> GetDocumentDetailsReportWithFastReport(ReportsViewForm req)
        {
            try
            {
                if (req.resultType != EResultType.Detailed)
                    return new BaseResponseDTOs(null, 400, "Invalid result type");

                // 1. Get user information
                var userId = (await _systemInfoServices.GetUserId()).Id;
                var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
                if (user == null)
                    return new BaseResponseDTOs(null, 500, "User not found");

                // 2. Build the query with filters
                var query = BuildFilteredQuery(req);

                // 3. Get total count for pagination info
                int page = req.pageNumber > 0 ? req.pageNumber : 1;
                int pageSize = req.pageSize > 0 ? req.pageSize : 10;
                int totalCount = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // 4. Get documents with related department data
                var pagedDocs = await query
                    .OrderByDescending(x => x.EditDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 5. Create data for FastReport
                var reportData = new List<DocumentReportDto>();
                foreach (var doc in pagedDocs)
                {
                    if (doc.FileType != null)
                    {
                        // Pre-fetch all department data needed for the documents in one query
                        var departmentIds = pagedDocs
                            .Where(d => d.DepartId.HasValue)
                            .Select(d => d.DepartId.Value)
                            .Distinct()
                            .ToList();

                        var departmentDict = await _context.GpDepartments
                            .Where(d => departmentIds.Contains(d.Id))
                            .ToDictionaryAsync(d => d.Id, d => d.Dscrp ?? "Unknown");

                        // Then inside your loop, use the dictionary instead of making a DB call
                        var department = doc.DepartId.HasValue && departmentDict.TryGetValue(doc.DepartId.Value, out var deptName)
                            ? deptName
                            : "Unknown";

                        reportData.Add(new DocumentReportDto
                        {
                            DepartmentId = doc.DepartId ?? 0,
                            DepartmentName = department,
                            Editor = doc.Editor ?? "N/A",
                            DocNo = doc.DocNo ?? "N/A",
                            Subject = doc.Subject ?? "N/A",
                            // Handle DateOnly to DateTime conversion
                            DocDate = doc.DocDate.HasValue ?
                                doc.DocDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                            EditDate = doc.EditDate,
                            Month = doc.DocDate?.Month ?? 0,
                            FilePath = doc.FileType.ToString() ?? string.Empty,
                            DocType = await GetDocTypeName(doc.DocType)
                        });
                    }
                }

                // 6. Generate report based on requested format
                if (req.outputFormat?.ToLower() == "pdf")
                {
                    byte[] reportBytes = GenerateDocumentReportWithFastReport(reportData, req.reportTitle ?? "Document Details Report");

                    return new BaseResponseDTOs(new
                    {
                        PdfData = Convert.ToBase64String(reportBytes),
                        FileName = "DocumentDetailsReport.pdf",
                        ContentType = "application/pdf"
                    }, 200, null);
                }
                else if (req.outputFormat?.ToLower() == "excel")
                {
                    // Generate PDF instead since Excel isn't supported/wanted
                    byte[] reportBytes = GenerateDocumentReportWithFastReport(reportData,
                        req.reportTitle ?? "Document Details Report (Excel not supported)");

                    return new BaseResponseDTOs(new
                    {
                        PdfData = Convert.ToBase64String(reportBytes),
                        FileName = "DocumentDetailsReport.pdf", // PDF extension instead of xlsx
                        ContentType = "application/pdf"         // PDF content type
                    }, 200, null);
                }
                else
                {
                    // Return JSON data for other formats
                    return new BaseResponseDTOs(new
                    {
                        Data = reportData,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        PageNumber = page,
                        PageSize = pageSize
                    }, 200, null);
                }
            }
            catch (Exception ex)
            {
                return new BaseResponseDTOs(null, 500, $"Error generating report: {ex.Message}");
            }
        }

        // Helper method to get document type name
        private async Task<string> GetDocTypeName(int docTypeId)
        {
            var docType = await _context.ArcivDocDscrps.FirstOrDefaultAsync(d => d.Id == docTypeId);
            return docType?.Dscrp ?? "Unknown";
        }

        // Generate PDF report using FastReport
        private byte[] GenerateDocumentReportWithFastReport(List<DocumentReportDto> data, string reportTitle)
        {
            using var report = new FastReport.Report();

            // 1. Register data source
            report.RegisterData(data, "Documents");

            // 2. Load report template or create one programmatically
            if (File.Exists("Reports/DocumentReport.frx"))
            {
                report.Load("Reports/DocumentReport.frx");
            }
            else
            {
                // Create report design programmatically
                report.ReportInfo.Name = reportTitle;

                // Add report page
                var page = new FastReport.ReportPage();
                report.Pages.Add(page);

                // Add title band
                var titleBand = new FastReport.DataBand();
                titleBand.Height = 30;
                page.Bands.Add(titleBand);

                // Add title text
                var titleText = new FastReport.TextObject();
                titleText.Text = reportTitle;
                titleText.Bounds = new RectangleF(0, 0, 720, 30);
                titleText.HorzAlign = FastReport.HorzAlign.Center;
                titleText.Font = new Font("Arial", 14, FontStyle.Bold);
                titleBand.Objects.Add(titleText);

                // Add header band
                var headerBand = new FastReport.DataBand();
                headerBand.Height = 30;
                page.Bands.Add(headerBand);

                // Add column headers
                CreateHeaderCell(headerBand, "Dept", 0, 100);
                CreateHeaderCell(headerBand, "Doc No", 100, 100);
                CreateHeaderCell(headerBand, "Subject", 200, 200);
                CreateHeaderCell(headerBand, "Date", 400, 80);
                CreateHeaderCell(headerBand, "Editor", 480, 100);
                CreateHeaderCell(headerBand, "Type", 580, 140);

                // Add data band
                var dataBand = new FastReport.DataBand();
                dataBand.Height = 25;
                dataBand.DataSource = report.GetDataSource("Documents");
                page.Bands.Add(dataBand);

                // Add data cells
                CreateDataCell(dataBand, "[Documents.DepartmentName]", 0, 100);
                CreateDataCell(dataBand, "[Documents.DocNo]", 100, 100);
                CreateDataCell(dataBand, "[Documents.Subject]", 200, 200);
                CreateDataCell(dataBand, "[Documents.DocDate]", 400, 80);
                CreateDataCell(dataBand, "[Documents.Editor]", 480, 100);
                CreateDataCell(dataBand, "[Documents.DocType]", 580, 140);
            }

            // 3. Prepare the report
            report.Prepare();

            // 4. Export to PDF
            using var ms = new MemoryStream();
            var pdfExport = new FastReport.Export.PdfSimple.PDFSimpleExport();
            pdfExport.Export(report, ms);
            return ms.ToArray();
        }

        // Helper method to create a header cell in FastReport
        private void CreateHeaderCell(FastReport.DataBand band, string text, float left, float width)
        {
            var cell = new FastReport.TextObject();
            cell.Text = text;
            cell.Bounds = new RectangleF(left, 0, width, 25);
            cell.Border.Lines = FastReport.BorderLines.All;
            cell.HorzAlign = FastReport.HorzAlign.Center;
            cell.VertAlign = FastReport.VertAlign.Center;
            cell.Font = new Font("Arial", 10, FontStyle.Bold);
            cell.FillColor = Color.LightGray;
            band.Objects.Add(cell);
        }

        // Helper method to create a data cell in FastReport
        private void CreateDataCell(FastReport.DataBand band, string dataField, float left, float width)
        {
            var cell = new FastReport.TextObject();
            cell.Text = dataField;
            cell.Bounds = new RectangleF(left, 0, width, 25);
            cell.Border.Lines = FastReport.BorderLines.All;
            cell.VertAlign = FastReport.VertAlign.Center;
            cell.Font = new Font("Arial", 9);
            band.Objects.Add(cell);
        }

        // Generate Excel report using FastReport
        private byte[] GenerateExcelReportWithFastReport(List<DocumentReportDto> data, string reportTitle)
        {
            // Since Excel export is not desired, use PDF format instead
            using var report = new FastReport.Report();
            report.RegisterData(data, "Documents");

            // Use the same report design as PDF
            if (File.Exists("Reports/DocumentReport.frx"))
            {
                report.Load("Reports/DocumentReport.frx");
            }
            else
            {
                // Create report design programmatically (same as in PDF method)
                report.ReportInfo.Name = reportTitle;

                // Add report page
                var page = new FastReport.ReportPage();
                report.Pages.Add(page);

                // Add title band
                var titleBand = new FastReport.DataBand();
                titleBand.Height = 30;
                page.Bands.Add(titleBand);

                // Add title text
                var titleText = new FastReport.TextObject();
                titleText.Text = reportTitle + " (PDF format - Excel export not supported)";
                titleText.Bounds = new RectangleF(0, 0, 720, 30);
                titleText.HorzAlign = FastReport.HorzAlign.Center;
                titleText.Font = new Font("Arial", 14, FontStyle.Bold);
                titleBand.Objects.Add(titleText);

                // Add header and data components as in PDF method
                var headerBand = new FastReport.DataBand();
                headerBand.Height = 30;
                page.Bands.Add(headerBand);

                CreateHeaderCell(headerBand, "Dept", 0, 100);
                CreateHeaderCell(headerBand, "Doc No", 100, 100);
                CreateHeaderCell(headerBand, "Subject", 200, 200);
                CreateHeaderCell(headerBand, "Date", 400, 80);
                CreateHeaderCell(headerBand, "Editor", 480, 100);
                CreateHeaderCell(headerBand, "Type", 580, 140);

                var dataBand = new FastReport.DataBand();
                dataBand.Height = 25;
                dataBand.DataSource = report.GetDataSource("Documents");
                page.Bands.Add(dataBand);

                CreateDataCell(dataBand, "[Documents.DepartmentName]", 0, 100);
                CreateDataCell(dataBand, "[Documents.DocNo]", 100, 100);
                CreateDataCell(dataBand, "[Documents.Subject]", 200, 200);
                CreateDataCell(dataBand, "[Documents.DocDate]", 400, 80);
                CreateDataCell(dataBand, "[Documents.Editor]", 480, 100);
                CreateDataCell(dataBand, "[Documents.DocType]", 580, 140);
            }

            // Prepare and export to PDF instead of Excel
            report.Prepare();

            using var ms = new MemoryStream();
            var pdfExport = new FastReport.Export.PdfSimple.PDFSimpleExport();
            pdfExport.Export(report, ms);
            return ms.ToArray();
        }

        // DTO for FastReport data
        public class DocumentReportDto
        {
            public int DepartmentId { get; set; }
            public string DepartmentName { get; set; } = "";
            public string Editor { get; set; } = "";
            public string DocNo { get; set; } = "";
            public string Subject { get; set; } = "";
            public DateTime? DocDate { get; set; }
            public DateTime? EditDate { get; set; }
            public int Month { get; set; }
            public string FilePath { get; set; } = "";
            public string DocType { get; set; } = "";
        }
        
        public async Task<BaseResponseDTOs> GetReferencedDocsCountsPagedAsync(ReportsViewForm req)
        {
            // Get filtered documents
            var filteredDocs = BuildFilteredQuery(req);

            // Only docs that are referenced as children
            var referencedDocs =
                from doc in filteredDocs
                join reference in _context.ArcivDocsRefrences on doc.RefrenceNo equals reference.LinkedRfrenceNo
                join dept in _context.GpDepartments on doc.DepartId equals dept.Id
                select new
                {
                    doc.Id,
                    doc.DocNo,
                    doc.RefrenceNo,
                    doc.DepartId,
                    DepartmentName = dept.Dscrp
                };

            // Group by department
            var departmentGroups = await referencedDocs
                .GroupBy(x => new { x.DepartId, x.DepartmentName })
                .Select(g => new
                {
                    DepartmentId = g.Key.DepartId,
                    DepartmentName = g.Key.DepartmentName,
                    DocsCount = g.Count(),
                })
                .OrderBy(x => x.DepartmentName)
                .ToListAsync();
            
            if (departmentGroups.Count == 0)
            {
                return new BaseResponseDTOs(new
                {
                    Departments = new List<object>(),
                    TotalCount = 0,
                    TotalPages = 0,
                    PageNumber = req.pageNumber,
                    PageSize = req.pageSize
                }, 200, null);
            }
            
            // Handle department-based pagination
            int page = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 10;
            
            // Calculate total pages based on department count
            int totalPages = (int)Math.Ceiling(departmentGroups.Count / (double)pageSize);
            
            // Get paged departments
            var pagedDepartments = departmentGroups
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Format response
            var response = new
            {
                Departments = pagedDepartments,
                TotalCount = departmentGroups.Count,
                TotalPages = totalPages,
                PageNumber = page,
                PageSize = pageSize,
                TotalDocsCount = departmentGroups.Sum(d => d.DocsCount)
            };

            return new BaseResponseDTOs(response, 200, null);
        }
    }
}
