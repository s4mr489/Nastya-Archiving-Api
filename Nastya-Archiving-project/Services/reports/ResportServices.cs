using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
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

        //public async Task<BaseResponseDTOs> GeneralResponse(ReportsViewForm req)
        //{
        //    var query = _context.ArcivingDocs.AsQueryable();

        //    if (req.fromEditingDate != null && req.toEditingDate != null)
        //    {
        //        query = query.Where(x => x.EditDate >= req.fromEditingDate && x.EditDate <= req.toEditingDate);
        //    }
        //    if (req.fromArchivingDate != null && req.toArchivingDate != null)
        //    {
        //        query = query.Where(x => x.DocDate >= req.fromArchivingDate && x.DocDate <= req.toArchivingDate);
        //    }

        //    if (req.docTypeId != null)
        //    {
        //        var (docsType, error) = await _archivingSettingsServicers.GetDocsTypeById(req.docTypeId.Value);
        //        if (docsType != null)
        //        {
        //            query = query.Where(x => x.DocType == req.docTypeId);
        //        }
        //    }
        //    if (req.sourceId != null)
        //    {
        //        var (source, error) = await _infrastructureServices.GetPOrganizationById(req.sourceId.Value);
        //        if (source != null)
        //        {
        //            query = query.Where(x => x.DocSource == req.sourceId);
        //        }
        //    }

        //    if (req.toId != null)
        //    {
        //        var (to, error) = await _infrastructureServices.GetPOrganizationById(req.toId.Value);
        //        if (to != null)
        //        {
        //            query = query.Where(x => x.DocSource == req.toId);
        //        }
        //    }

        //    if (req.departmentId != null && req.departmentId.Count > 0)
        //    {
        //        query = query.Where(x => req.departmentId.Contains(x.DepartId));
        //    }

        //    if (req.reportType != null)
        //    {
        //        switch (req.reportType)
        //        {
        //            case EReportType.GeneralReport:

        //                switch (req.resultType)
        //                {
        //                    case EResultType.statistical:
        //                        // Pagination parameters (customize as needed)
        //                        int page = req.pageNumber > 0 ? req.pageNumber : 1;
        //                        int pageSize = req.pageSize > 0 ? req.pageSize : 10;

        //                        // Get total count before paging
        //                        int totalCount = await query.CountAsync();
        //                        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        //                        if (totalCount == 0)
        //                        {
        //                            var emptyResponse = new
        //                            {
        //                                Data = new List<object>(),
        //                                TotalCount = 0,
        //                                TotalPages = 0
        //                            };
        //                            return new BaseResponseDTOs(emptyResponse, 200, null);
        //                        }

        //                        // Apply paging
        //                        var result = await query
        //                            .Select(x => new
        //                            {
        //                                x.DocType,
        //                                x.Subject,
        //                                x.DocSize,
        //                                x.FileType,
        //                                Editor = x.Editor != null ? _encryptionServices.DecryptString256Bit(x.Editor) : null,
        //                                x.DocDate
        //                            })
        //                            .Skip((page - 1) * pageSize)
        //                            .Take(pageSize)
        //                            .ToListAsync();

        //                        var responseData = new
        //                        {
        //                            Data = result,
        //                            TotalCount = totalCount,
        //                            TotalPages = totalPages
        //                        };

        //                        return new BaseResponseDTOs(responseData, 200, null);

        //                    case EResultType.Detailed:
        //                        query = query.Where(x => x.SubDocType == 1);
        //                        break;
        //                    default:
        //                        throw new ArgumentOutOfRangeException(nameof(req.resultType), "Invalid result type");
        //                }
        //                break;
        //            case EReportType.BySendingOrgnization:
        //                break;
        //            case EReportType.ByDepartmentAndUsers:
        //                // No filter needed
        //                break;
        //            default:
        //                throw new ArgumentOutOfRangeException(nameof(req.reportType), "Invalid report type");
        //        }

        //        return new BaseResponseDTOs(null, 400, "eror");
        //    }

        //    // Add a default return statement to cover all code paths
        //    return new BaseResponseDTOs(null, 400, "Invalid request");
        //}
        public async Task<BaseResponseDTOs> GeneralReport(ReportsViewForm req)
        {
            var query = _context.ArcivingDocs.AsQueryable();

            if (req.fromEditingDate != null && req.toEditingDate != null)
            {
                query = query.Where(x => x.EditDate >= req.fromEditingDate && x.EditDate <= req.toEditingDate);
            }
            if (req.fromArchivingDate != null && req.toArchivingDate != null)
            {
                query = query.Where(x => x.DocDate >= req.fromArchivingDate && x.DocDate <= req.toArchivingDate);
            }

            if (req.docTypeId != null)
            {
                var (docsType, error) = await _archivingSettingsServicers.GetDocsTypeById(req.docTypeId.Value);
                if (docsType != null)
                {
                    query = query.Where(x => x.DocType == req.docTypeId);
                }
            }
            if (req.sourceId != null)
            {
                var (source, error) = await _infrastructureServices.GetPOrganizationById(req.sourceId.Value);
                if (source != null)
                {
                    query = query.Where(x => x.DocSource == req.sourceId);
                }
            }

            if (req.toId != null)
            {
                var (to, error) = await _infrastructureServices.GetPOrganizationById(req.toId.Value);
                if (to != null)
                {
                    query = query.Where(x => x.DocSource == req.toId);
                }
            }

            // If searching for departments, return zero if not found
            if (req.departmentId != null && req.departmentId.Count > 0)
            {
                var found = await query.AnyAsync(x => req.departmentId.Contains(x.DepartId));
                if (!found)
                {
                    var emptyResponse = new
                    {
                        Data = new List<object>(),
                        TotalCount = 0,
                        TotalPages = 0,
                        PageNumber = req.pageNumber > 0 ? req.pageNumber : 1,
                        PageSize = req.pageSize > 0 ? req.pageSize : 10
                    };
                    return new BaseResponseDTOs(emptyResponse, 200, null);
                }
                query = query.Where(x => req.departmentId.Contains(x.DepartId));
            }

            // Pagination
            int page = req.pageNumber > 0 ? req.pageNumber : 1;
            int pageSize = req.pageSize > 0 ? req.pageSize : 10;
            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var docs = await query
                .OrderBy(x => x.DepartId)
                .Select(x => new {
                    x.Id,
                    x.DocType,
                    x.Subject,
                    x.DocSize,
                    x.FileType,
                    x.Editor,
                    x.DocDate,
                    x.EditDate,
                    x.DepartId,
                    x.DocSource
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Enrich the data in memory
            var enrichedDocs = new List<dynamic>();
            foreach (var doc in docs)
            {
                var (docType, _) = await _archivingSettingsServicers.GetDocsTypeById(doc.DocType);
                enrichedDocs.Add(new
                {
                    doc.Id,
                    doc.DocType,
                    doc.Subject,
                    doc.DocSize,
                    doc.FileType,
                    doc.Editor,
                    doc.DocDate,
                    doc.EditDate,
                    doc.DepartId,
                    doc.DocSource,
                    docTpye = docType?.docuName
                });
            }

            // Group by department
            var grouped = enrichedDocs
                .GroupBy(d => d.DepartId)
                .Select(g => new
                {
                    DepartId = g.Key,
                    Documents = g.ToList()
                })
                .ToList();

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
    }
    
}
