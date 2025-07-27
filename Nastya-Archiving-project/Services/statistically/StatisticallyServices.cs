using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
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

            // Optional: filter by department if provided
            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => x.DepartId.HasValue && req.departmentId.Contains(x.DepartId));
            if (req.year != null && req.year > 0)
                query = query.Where(x => x.DocDate.HasValue && x.DocDate.Value.Year == req.year);

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

            // Optional: filter by department if provided
            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => x.DepartId.HasValue && req.departmentId.Contains(x.DepartId));
            if (req.year != null && req.year > 0)
                query = query.Where(x => x.DocDate.HasValue && x.DocDate.Value.Year == req.year);

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

            // Optional: filter by department if provided
            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => x.DepartId.HasValue && req.departmentId.Contains(x.DepartId));
            if (req.year != null && req.year > 0)
                query = query.Where(x => x.DocDate.HasValue && x.DocDate.Value.Year == req.year);

            // Only consider docs with a valid DocType
            var grouped = await query
                .Where(x => x.DocType > 0)
                .GroupBy(x => x.DocType)
                .Select(g => new
                {
                    DocTypeId = g.Key,
                    Count = g.Count(),
                })
                .OrderBy(x => x.DocTypeId)
                .ToListAsync();

            // Get all doc types for mapping id to name
            var (docTypes, _) = await archivingSettingsServicers.GetAllDocsTypes();
            var docTypeNames = (docTypes ?? new List<DocTypeResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.docuName);

            var resultWithNames = grouped.Select(x => new
            {
                DocTypeId = x.DocTypeId,
                DocTypeName = docTypeNames.ContainsKey(x.DocTypeId) ? docTypeNames[x.DocTypeId] : null,
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

            // Optional: filter by department if provided
            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => x.DepartId.HasValue && req.departmentId.Contains(x.DepartId));
            if (req.year != null && req.year > 0)
                query = query.Where(x => x.DocDate.HasValue && x.DocDate.Value.Year == req.year);

            // Only consider docs with a DocTarget
            var grouped = await query
                .Where(x => x.DocTarget.HasValue && x.DocTarget.Value > 0)
                .GroupBy(x => x.DocTarget.Value)
                .Select(g => new
                {
                    DocTargetId = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.DocTargetId)
                .ToListAsync();

            // Get all organizations for mapping id to name
            var (orgs, _) = await _infrastructureServices.GetAllPOrganizations();
            var orgNames = (orgs ?? new List<OrgniztionResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.Dscrp);

            var resultWithNames = grouped.Select(x => new
            {
                DocTargetId = x.DocTargetId,
                DocTargetName = orgNames.ContainsKey(x.DocTargetId) ? orgNames[x.DocTargetId] : null,
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

        public async Task<BaseResponseDTOs> GetDocumentByOrgniztion(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            // Optional: filter by department if provided
            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => x.DepartId.HasValue && req.departmentId.Contains(x.DepartId));
            if (req.year != null && req.year > 0)
                query = query.Where(x => x.DocDate.HasValue && x.DocDate.Value.Year == req.year);

            // Only consider docs with a DocSource
            var grouped = await query
                .Where(x => x.DocSource.HasValue && x.DocSource.Value > 0)
                .GroupBy(x => x.DocSource.Value)
                .Select(g => new
                {
                    DocSourceId = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.DocSourceId)
                .ToListAsync();

            // Get all organizations for mapping id to name
            var (orgs, _) = await _infrastructureServices.GetAllPOrganizations();
            var orgNames = (orgs ?? new List<OrgniztionResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.Dscrp);

            var resultWithNames = grouped.Select(x => new
            {
                DocSourceId = x.DocSourceId,
                DocSourceName = orgNames.ContainsKey(x.DocSourceId) ? orgNames[x.DocSourceId] : null,
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

            // Optional: filter by department if provided
            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => x.DepartId.HasValue && req.departmentId.Contains(x.DepartId));
            if (req.year != null && req.year > 0)
                query = query.Where(x => x.DocDate.HasValue && x.DocDate.Value.Year == req.year);

            // Only consider docs with a SupDocType
            var grouped = await query
                .Where(x => x.SubDocType.HasValue && x.SubDocType.Value > 0)
                .GroupBy(x => x.SubDocType.Value)
                .Select(g => new
                {
                    SupDocTypeId = g.Key,
                    Count = g.Count(),
                    
                })
                .OrderBy(x => x.SupDocTypeId)
                .ToListAsync();

            // Get all sup doc types for mapping id to name
            var (supDocTypes, _) = await archivingSettingsServicers.GetAllSupDocsTypes();
            var supDocTypeNames = (supDocTypes ?? new List<SupDocsTypeResponseDTOs>())
                .ToDictionary(x => x.Id, x => x.supDocuName);

            var resultWithNames = grouped.Select(x => new
            {
                SupDocTypeId = x.SupDocTypeId,
                SupDocTypeName = supDocTypeNames.ContainsKey(x.SupDocTypeId) ? supDocTypeNames[x.SupDocTypeId] : null,
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

        public async Task<BaseResponseDTOs> GetFileSizeUplodedByMonthAsync(StatisticallyViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();
            if (req.year != null && req.year > 0)
                query = query.Where(x => x.DocDate.HasValue && x.DocDate.Value.Year == req.year);

            // Optional: filter by department if provided
            if (req.departmentId != null && req.departmentId.Count > 0)
                query = query.Where(x => x.DepartId.HasValue && req.departmentId.Contains(x.DepartId));

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
    }
}
