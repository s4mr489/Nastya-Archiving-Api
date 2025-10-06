using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.DocsType;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.SupDocsType;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Organization;
using Nastya_Archiving_project.Models.DTOs.Statistically;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.infrastructure;

namespace Nastya_Archiving_project.Services.statistically
{
    public class StatisticallyServices : BaseServices,IStatisticallyServices
    {
        private readonly AppDbContext _context;
        private readonly IInfrastructureServices _infrastructureServices;
        private readonly IArchivingSettingsServicers archivingSettingsServicers;
        public StatisticallyServices(AppDbContext context, 
                                    IInfrastructureServices infrastructureServices, 
                                    IArchivingSettingsServicers archivingSettingsServicers) : base(null, context)
        {
            _context = context;
            _infrastructureServices = infrastructureServices;
            this.archivingSettingsServicers = archivingSettingsServicers;
        }

        public async Task<BaseResponseDTOs> GetFileCountByEditorAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            var grouped = await query
                .Where(x => x.DepartId.HasValue && !string.IsNullOrEmpty(x.Editor))
                .GroupBy(x => new { x.DepartId, x.Editor })
                .Select(g => new
                {
                    DepartmentId = g.Key.DepartId,
                    Editor = g.Key.Editor,
                    Count = g.Count()
                })
                .OrderBy(x => x.DepartmentId)
                .ThenBy(x => x.Editor)
                .ToListAsync();

            // Get all department names for the involved DepartmentIds
            var departmentIds = grouped.Select(x => x.DepartmentId).Distinct().ToList();
            var (departments, _) = await _infrastructureServices.GetAllDepartment();
            var departmentNames = (departments ?? new List<DepartmentResponseDTOs>())
                .ToDictionary(d => d.Id, d => d.DepartmentName);

            var resultWithNames = grouped.Select(x => new
            {
                DepartmentId = x.DepartmentId,
                DepartmentName = x.DepartmentId.HasValue && departmentNames.ContainsKey(x.DepartmentId.Value)
                    ? departmentNames[x.DepartmentId.Value]
                    : null,
                x.Editor,
                x.Count
            }).ToList();

            // Filter results based on outputType if specified
            if (req.outputType.HasValue)
            {
                if (req.outputType == OutputType.DepartmentsOnly)
                {
                    var departmentGroups = resultWithNames
                        .GroupBy(x => new { x.DepartmentId, x.DepartmentName })
                        .Select(g => new
                        {
                            DepartmentId = g.Key.DepartmentId,
                            DepartmentName = g.Key.DepartmentName,
                            Count = g.Sum(x => x.Count)
                        })
                        .ToList();

                    return new BaseResponseDTOs(new
                    {
                        Total = departmentGroups.Sum(x => x.Count),
                        Data = departmentGroups
                    }, 200, null);
                }
                else if (req.outputType == OutputType.EmployeesOnly)
                {
                    var employeeGroups = resultWithNames
                        .GroupBy(x => x.Editor)
                        .Select(g => new
                        {
                            Editor = g.Key,
                            Count = g.Sum(x => x.Count)
                        })
                        .ToList();

                    return new BaseResponseDTOs(new
                    {
                        Total = employeeGroups.Sum(x => x.Count),
                        Data = employeeGroups
                    }, 200, null);
                }
            }

            var total = grouped.Sum(x => x.Count);

            var response = new
            {
                Total = total,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetCountByMonthAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            var grouped = query
                .Where(x => x.EditDate.HasValue && !string.IsNullOrEmpty(x.Editor) && x.DepartId.HasValue)
                .GroupBy(x => new { x.EditDate.Value.Year, x.EditDate.Value.Month, x.Editor, x.DepartId })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Editor = g.Key.Editor,
                    DepartmentId = g.Key.DepartId,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ThenBy(x => x.DepartmentId)
                .ThenBy(x => x.Editor)
                .ToList();

            // Get all department names for the involved DepartmentIds
            var departmentIds = grouped.Select(x => x.DepartmentId).Distinct().ToList();
            var departmentsTask = _infrastructureServices.GetAllDepartment();
            departmentsTask.Wait();
            var departments = departmentsTask.Result.Department ?? new List<DepartmentResponseDTOs>();

            // Map DepartmentId to DepartmentName
            var departmentNames = departments
                .Where(d => d != null && d.Id != null)
                .ToDictionary(d => d.Id, d => d.DepartmentName);

            var resultWithNames = grouped.Select(x => new
            {
                x.Year,
                x.Month,
                x.Editor,
                x.DepartmentId,
                DepartmentName = x.DepartmentId.HasValue && departmentNames.ContainsKey(x.DepartmentId.Value)
                    ? departmentNames[x.DepartmentId.Value]
                    : null,
                x.Count
            }).ToList();

            // Filter results based on outputType if specified
            if (req.outputType.HasValue)
            {
                if (req.outputType == OutputType.DepartmentsOnly)
                {
                    var departmentGroups = resultWithNames
                        .GroupBy(x => new { x.Year, x.Month, x.DepartmentId, x.DepartmentName })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Month,
                            g.Key.DepartmentId,
                            g.Key.DepartmentName,
                            Count = g.Sum(x => x.Count)
                        })
                        .ToList();

                    return new BaseResponseDTOs(new
                    {
                        Total = departmentGroups.Sum(x => x.Count),
                        Data = departmentGroups
                    }, 200, null);
                }
                else if (req.outputType == OutputType.EmployeesOnly)
                {
                    var employeeGroups = resultWithNames
                        .GroupBy(x => new { x.Year, x.Month, x.Editor })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.Month,
                            g.Key.Editor,
                            Count = g.Sum(x => x.Count)
                        })
                        .ToList();

                    return new BaseResponseDTOs(new
                    {
                        Total = employeeGroups.Sum(x => x.Count),
                        Data = employeeGroups
                    }, 200, null);
                }
            }

            var total = grouped.Sum(x => x.Count);

            var response = new
            {
                Total = total,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetDocumentByDocType(StatisticallyViewForm req)
        { 
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Only consider docs with a valid DocType
            var grouped = await query
                .Where(x => x.DocType > 0)
                .GroupBy(x => new { x.DocType, Year = x.EditDate.HasValue ? x.EditDate.Value.Year : 0, Month = x.EditDate.HasValue ? x.EditDate.Value.Month : 0 })
                .Select(g => new
                {
                    DocTypeId = g.Key.DocType,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                })
                .OrderBy(x => x.DocTypeId)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Get all doc types for mapping id to name
            var (docTypes, _) = await archivingSettingsServicers.GetAllDocsTypes();
            var docTypeNames = (docTypes ?? new List<DocTypeResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.docuName);

            var resultWithNames = grouped.Select(x => new
            {
                DocTypeId = x.DocTypeId,
                DocTypeName = docTypeNames.ContainsKey(x.DocTypeId) ? docTypeNames[x.DocTypeId] : null,
                x.Year,
                x.Month,
                x.Count,
            }).ToList();

            var total = grouped.Sum(x => x.Count);

            var response = new
            {
                Total = total,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetDocumentByDocTargetAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Only consider docs with a DocTarget and DocSize
            var grouped = await query
                .Where(x => x.DocTarget.HasValue && x.DocTarget.Value > 0 && x.DocSize.HasValue)
                .GroupBy(x => new { x.DocTarget.Value, Year = x.EditDate.HasValue ? x.EditDate.Value.Year : 0, Month = x.EditDate.HasValue ? x.EditDate.Value.Month : 0 })
                .Select(g => new
                {
                    DocTargetId = g.Key.Value,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSize = g.Sum(x => x.DocSize)
                })
                .OrderBy(x => x.DocTargetId)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Get all organizations for mapping id to name
            var (orgs, _) = await _infrastructureServices.GetAllPOrganizations();
            var orgNames = (orgs ?? new List<OrgniztionResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.Dscrp);

            var resultWithNames = grouped.Select(x => new
            {
                DocTargetId = x.DocTargetId,
                DocTargetName = orgNames.ContainsKey(x.DocTargetId) ? orgNames[x.DocTargetId] : null,
                x.Year,
                x.Month,
                x.TotalSize
            }).ToList();

            var totalSize = grouped.Sum(x => x.TotalSize);

            var response = new
            {
                TotalSize = totalSize,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetDocumentByOrgniztion(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Only consider docs with a DocSource
            var grouped = await query
                .Where(x => x.DocSource.HasValue && x.DocSource.Value > 0)
                .GroupBy(x => new { x.DocSource.Value, Year = x.EditDate.HasValue ? x.EditDate.Value.Year : 0, Month = x.EditDate.HasValue ? x.EditDate.Value.Month : 0 })
                .Select(g => new
                {
                    DocSourceId = g.Key.Value,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.DocSourceId)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Get all organizations for mapping id to name
            var (orgs, _) = await _infrastructureServices.GetAllPOrganizations();
            var orgNames = (orgs ?? new List<OrgniztionResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.Dscrp);

            var resultWithNames = grouped.Select(x => new
            {
                DocSourceId = x.DocSourceId,
                DocSourceName = orgNames.ContainsKey(x.DocSourceId) ? orgNames[x.DocSourceId] : null,
                x.Year,
                x.Month,
                x.Count
            }).ToList();

            var total = grouped.Sum(x => x.Count);

            var response = new
            {
                Total = total,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetDocumentBySupDocTpye(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Only consider docs with a SupDocType
            var grouped = await query
                .Where(x => x.SubDocType.HasValue && x.SubDocType.Value > 0)
                .GroupBy(x => new { x.SubDocType.Value, Year = x.EditDate.HasValue ? x.EditDate.Value.Year : 0, Month = x.EditDate.HasValue ? x.EditDate.Value.Month : 0 })
                .Select(g => new
                {
                    SupDocTypeId = g.Key.Value,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.SupDocTypeId)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Get all sup doc types for mapping id to name
            var (supDocTypes, _) = await archivingSettingsServicers.GetAllSupDocsTypes();
            var supDocTypeNames = (supDocTypes ?? new List<SupDocsTypeResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.supDocuName);

            var resultWithNames = grouped.Select(x => new
            {
                SupDocTypeId = x.SupDocTypeId,
                SupDocTypeName = supDocTypeNames.ContainsKey(x.SupDocTypeId) ? supDocTypeNames[x.SupDocTypeId] : null,
                x.Year,
                x.Month,
                x.Count
            }).ToList();

            var total = grouped.Sum(x => x.Count);

            var response = new
            {
                Total = total,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetFileSizeUplodedByMonthAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();
            
            // Apply filters
            query = ApplyCommonFilters(query, req);

            var grouped = await query
                .Where(x => x.DocDate.HasValue && x.DocSize.HasValue)
                .GroupBy(x => new { x.DocDate.Value.Year, x.DocDate.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalDocSize = g.Sum(x => x.DocSize) // Sum of DocSize for the month
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var totalDocSize = grouped.Sum(x => x.TotalDocSize);

            var response = new
            {
                TotalDocSize = totalDocSize,
                Data = grouped
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        // New methods implementation based on requirements

        public async Task<BaseResponseDTOs> GetFileSizeByEditorAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            var grouped = await query
                .Where(x => x.DepartId.HasValue && !string.IsNullOrEmpty(x.Editor) && x.DocSize.HasValue)
                .GroupBy(x => new { x.DepartId, x.Editor })
                .Select(g => new
                {
                    DepartmentId = g.Key.DepartId,
                    Editor = g.Key.Editor,
                    TotalSize = g.Sum(x => x.DocSize)
                })
                .OrderBy(x => x.DepartmentId)
                .ThenBy(x => x.Editor)
                .ToListAsync();

            // Get all department names for the involved DepartmentIds
            var departmentIds = grouped.Select(x => x.DepartmentId).Distinct().ToList();
            var (departments, _) = await _infrastructureServices.GetAllDepartment();
            var departmentNames = (departments ?? new List<DepartmentResponseDTOs>())
                .ToDictionary(d => d.Id, d => d.DepartmentName);

            var resultWithNames = grouped.Select(x => new
            {
                DepartmentId = x.DepartmentId,
                DepartmentName = x.DepartmentId.HasValue && departmentNames.ContainsKey(x.DepartmentId.Value)
                    ? departmentNames[x.DepartmentId.Value]
                    : null,
                x.Editor,
                TotalSize = x.TotalSize
            }).ToList();

            // Filter results based on outputType if specified
            if (req.outputType.HasValue)
            {
                if (req.outputType == OutputType.DepartmentsOnly)
                {
                    var departmentGroups = resultWithNames
                        .GroupBy(x => new { x.DepartmentId, x.DepartmentName })
                        .Select(g => new
                        {
                            DepartmentId = g.Key.DepartmentId,
                            DepartmentName = g.Key.DepartmentName,
                            TotalSize = g.Sum(x => x.TotalSize)
                        })
                        .ToList();

                    return new BaseResponseDTOs(new
                    {
                        TotalSize = departmentGroups.Sum(x => x.TotalSize),
                        Data = departmentGroups
                    }, 200, null);
                }
                else if (req.outputType == OutputType.EmployeesOnly)
                {
                    var employeeGroups = resultWithNames
                        .GroupBy(x => x.Editor)
                        .Select(g => new
                        {
                            Editor = g.Key,
                            TotalSize = g.Sum(x => x.TotalSize)
                        })
                        .ToList();

                    return new BaseResponseDTOs(new
                    {
                        TotalSize = employeeGroups.Sum(x => x.TotalSize),
                        Data = employeeGroups
                    }, 200, null);
                }
            }

            var totalSize = grouped.Sum(x => x.TotalSize);

            var response = new
            {
                TotalSize = totalSize,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetFileSizeByOrgnizationAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Only consider docs with a DocSource and DocSize
            var grouped = await query
                .Where(x => x.DocSource.HasValue && x.DocSource.Value > 0 && x.DocSize.HasValue)
                .GroupBy(x => new { x.DocSource.Value, Year = x.EditDate.HasValue ? x.EditDate.Value.Year : 0, Month = x.EditDate.HasValue ? x.EditDate.Value.Month : 0 })
                .Select(g => new
                {
                    DocSourceId = g.Key.Value,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSize = g.Sum(x => x.DocSize)
                })
                .OrderBy(x => x.DocSourceId)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Get all organizations for mapping id to name
            var (orgs, _) = await _infrastructureServices.GetAllPOrganizations();
            var orgNames = (orgs ?? new List<OrgniztionResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.Dscrp);

            var resultWithNames = grouped.Select(x => new
            {
                DocSourceId = x.DocSourceId,
                DocSourceName = orgNames.ContainsKey(x.DocSourceId) ? orgNames[x.DocSourceId] : null,
                x.Year,
                x.Month,
                x.TotalSize
            }).ToList();

            var totalSize = grouped.Sum(x => x.TotalSize);

            var response = new
            {
                TotalSize = totalSize,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetFileSizeByDocTargetAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Only consider docs with a DocTarget and DocSize
            var grouped = await query
                .Where(x => x.DocTarget.HasValue && x.DocTarget.Value > 0 && x.DocSize.HasValue)
                .GroupBy(x => new { x.DocTarget.Value, Year = x.EditDate.HasValue ? x.EditDate.Value.Year : 0, Month = x.EditDate.HasValue ? x.EditDate.Value.Month : 0 })
                .Select(g => new
                {
                    DocTargetId = g.Key.Value,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSize = g.Sum(x => x.DocSize)
                })
                .OrderBy(x => x.DocTargetId)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Get all organizations for mapping id to name
            var (orgs, _) = await _infrastructureServices.GetAllPOrganizations();
            var orgNames = (orgs ?? new List<OrgniztionResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.Dscrp);

            var resultWithNames = grouped.Select(x => new
            {
                DocTargetId = x.DocTargetId,
                DocTargetName = orgNames.ContainsKey(x.DocTargetId) ? orgNames[x.DocTargetId] : null,
                x.Year,
                x.Month,
                x.TotalSize
            }).ToList();

            var totalSize = grouped.Sum(x => x.TotalSize);

            var response = new
            {
                TotalSize = totalSize,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetFileSizeByDocTypeAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Only consider docs with a valid DocType and DocSize
            var grouped = await query
                .Where(x => x.DocType > 0 && x.DocSize.HasValue)
                .GroupBy(x => new { x.DocType, Year = x.EditDate.HasValue ? x.EditDate.Value.Year : 0, Month = x.EditDate.HasValue ? x.EditDate.Value.Month : 0 })
                .Select(g => new
                {
                    DocTypeId = g.Key.DocType,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSize = g.Sum(x => x.DocSize),
                })
                .OrderBy(x => x.DocTypeId)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Get all doc types for mapping id to name
            var (docTypes, _) = await archivingSettingsServicers.GetAllDocsTypes();
            var docTypeNames = (docTypes ?? new List<DocTypeResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.docuName);

            var resultWithNames = grouped.Select(x => new
            {
                DocTypeId = x.DocTypeId,
                DocTypeName = docTypeNames.ContainsKey(x.DocTypeId) ? docTypeNames[x.DocTypeId] : null,
                x.Year,
                x.Month,
                x.TotalSize,
            }).ToList();

            var totalSize = grouped.Sum(x => x.TotalSize);

            var response = new
            {
                TotalSize = totalSize,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> GetFileSizeBySupDocTypeAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Only consider docs with a SupDocType and DocSize
            var grouped = await query
                .Where(x => x.SubDocType.HasValue && x.SubDocType.Value > 0 && x.DocSize.HasValue)
                .GroupBy(x => new { x.SubDocType.Value, Year = x.EditDate.HasValue ? x.EditDate.Value.Year : 0, Month = x.EditDate.HasValue ? x.EditDate.Value.Month : 0 })
                .Select(g => new
                {
                    SupDocTypeId = g.Key.Value,
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSize = g.Sum(x => x.DocSize),
                })
                .OrderBy(x => x.SupDocTypeId)
                .ThenBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Get all sup doc types for mapping id to name
            var (supDocTypes, _) = await archivingSettingsServicers.GetAllSupDocsTypes();
            var supDocTypeNames = (supDocTypes ?? new List<SupDocsTypeResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.supDocuName);

            var resultWithNames = grouped.Select(x => new
            {
                SupDocTypeId = x.SupDocTypeId,
                SupDocTypeName = supDocTypeNames.ContainsKey(x.SupDocTypeId) ? supDocTypeNames[x.SupDocTypeId] : null,
                x.Year,
                x.Month,
                x.TotalSize,
            }).ToList();

            var totalSize = grouped.Sum(x => x.TotalSize);

            var response = new
            {
                TotalSize = totalSize,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        public async Task<BaseResponseDTOs> CompareDocDateWithEditDateAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Apply filters
            query = ApplyCommonFilters(query, req);

            // Get documents with same and different dates
            var docs = await query
                .Where(x => x.DocDate.HasValue && x.EditDate.HasValue)
                .Select(x => new
                {
                    x.Id,
                    x.DocType,
                    DocDate = x.DocDate,
                    EditDate = x.EditDate,
                    IsSameDate = new DateOnly(x.EditDate.Value.Year, x.EditDate.Value.Month, x.EditDate.Value.Day) == x.DocDate.Value
                })
                .ToListAsync();

            // Group by document type and whether dates match
            var grouped = docs
                .GroupBy(x => new { x.DocType, x.IsSameDate })
                .Select(g => new
                {
                    DocTypeId = g.Key.DocType,
                    IsSameDate = g.Key.IsSameDate,
                    Count = g.Count()
                })
                .OrderBy(x => x.DocTypeId)
                .ThenBy(x => x.IsSameDate ? 0 : 1)
                .ToList();

            // Get all doc types for mapping id to name
            var (docTypes, _) = await archivingSettingsServicers.GetAllDocsTypes();
            var docTypeNames = (docTypes ?? new List<DocTypeResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.docuName);

            var resultWithNames = grouped.Select(x => new
            {
                DocTypeId = x.DocTypeId,
                DocTypeName = docTypeNames.ContainsKey(x.DocTypeId) ? docTypeNames[x.DocTypeId] : null,
                DateMatch = x.IsSameDate ? "Same Date" : "Different Date",
                x.Count
            }).ToList();

            var sameDateTotal = grouped.Where(x => x.IsSameDate).Sum(x => x.Count);
            var differentDateTotal = grouped.Where(x => !x.IsSameDate).Sum(x => x.Count);
            var total = sameDateTotal + differentDateTotal;

            var response = new
            {
                Total = total,
                SameDateTotal = sameDateTotal,
                DifferentDateTotal = differentDateTotal,
                Data = resultWithNames
            };

            return new BaseResponseDTOs(response, 200, null);
        }

        // Helper method to apply common filters to queries
        private IQueryable<ArcivingDoc> ApplyCommonFilters(IQueryable<ArcivingDoc> query, StatisticallyViewForm req)
        {
            // Filter by department if provided
            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => x.DepartId.HasValue && req.departmentId.Contains(x.DepartId));

            // Filter by source organization if provided
            if (req.docSourceId != null && req.docSourceId.Count > 0)
                query = query.Where(x => x.DocSource.HasValue && req.docSourceId.Contains(x.DocSource));

            // Filter by target organization if provided
            if (req.docTargetId != null && req.docTargetId.Count > 0)
                query = query.Where(x => x.DocTarget.HasValue && req.docTargetId.Contains(x.DocTarget));

            // Filter by document type if provided
            if (req.docTypeId != null && req.docTypeId.Count > 0)
                query = query.Where(x => req.docTypeId.Contains(x.DocType));

            // Filter by supplementary document type if provided
            if (req.supDocTypeId != null && req.supDocTypeId.Count > 0)
                query = query.Where(x => x.SubDocType.HasValue && req.supDocTypeId.Contains(x.SubDocType));

            // Filter by specific editors if provided
            if (req.editorIds != null && req.editorIds.Count > 0)
                query = query.Where(x => req.editorIds.Contains(x.Editor));

            // Filter by year - use a more precise approach
            if (req.year != null && req.year > 0)
            {
                query = query.Where(x => 
                    (x.EditDate.HasValue && x.EditDate.Value.Year == req.year) || 
                    (x.DocDate.HasValue && x.DocDate.Value.Year == req.year) ||
                    (x.Theyear.HasValue && x.Theyear == req.year));
            }

            // Filter by month - use a more precise approach
            if (req.month != null && req.month > 0)
            {
                query = query.Where(x => 
                    (x.EditDate.HasValue && x.EditDate.Value.Month == req.month) ||
                    (x.DocDate.HasValue && x.DocDate.Value.Month == req.month) ||
                    (x.TheMonth.HasValue && x.TheMonth == req.month));
            }

            // Filter by date range for editing date
            if (req.fromEditingDate.HasValue)
                query = query.Where(x => x.EditDate.HasValue && x.EditDate.Value >= req.fromEditingDate.Value);

            if (req.toEditingDate.HasValue)
                query = query.Where(x => x.EditDate.HasValue && x.EditDate.Value <= req.toEditingDate.Value);

            // Filter by date range for document date
            if (req.fromDocDate.HasValue)
            {
                var fromDate = req.fromDocDate.Value.Date;
                query = query.Where(x => x.DocDate.HasValue && 
                    new DateTime(x.DocDate.Value.Year, x.DocDate.Value.Month, x.DocDate.Value.Day) >= fromDate);
            }

            if (req.toDocDate.HasValue)
            {
                var toDate = req.toDocDate.Value.Date;
                query = query.Where(x => x.DocDate.HasValue && 
                    new DateTime(x.DocDate.Value.Year, x.DocDate.Value.Month, x.DocDate.Value.Day) <= toDate);
            }

            return query;
        }
    }
}
