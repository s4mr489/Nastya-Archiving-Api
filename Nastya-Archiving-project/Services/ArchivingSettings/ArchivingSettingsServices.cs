using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.ArchivingPoint;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.DocsType;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.Precedence;
using Nastya_Archiving_project.Models.DTOs.ArchivingSettings.SupDocsType;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;
using Org.BouncyCastle.Crypto.Engines;
using System.Security.AccessControl;

namespace Nastya_Archiving_project.Services.ArchivingSettings
{
    public class ArchivingSettingsServices : BaseServices, IArchivingSettingsServicers
    { 
        private readonly AppDbContext _context;
        private readonly  InfrastructureServices _infrastucatre;
        private readonly ISystemInfoServices _systemInfoServices;
        public ArchivingSettingsServices(InfrastructureServices infrastucatre, AppDbContext context, ISystemInfoServices systemInfoServices) : base(null, context)
        {
            _infrastucatre = infrastucatre;
            _context = context;
            _systemInfoServices = systemInfoServices;
        }

        //Archiving Point Login Implementation

        public async Task<(ArchivingPointResponseDTOs? point, string? error)> PostArchivingPoint(ArchivingPointViewForm req)
        {
            var point = await _context.PArcivingPoints.FirstOrDefaultAsync(e => e.Dscrp == req.pointName );
            if(point != null)
                return (null, "400"); // Archiving point already exists

            var branch = await _infrastucatre.GetBranchById(req.branchId);
            if (branch.Branch == null)
                return (null , "404");
            var unit = await _infrastucatre.GetAccountUintById(req.accountUnitId);
            if (unit.accountUnits == null)
                return (null, "404");
            var depart = await _infrastucatre.GetDepartmentById(req.departmentId);
            if (depart.Department== null)
                return (null , "404"); 

            point = new PArcivingPoint
            {
                AccountUnitId = req.accountUnitId,
                BackupPath = unit.accountUnits.BackupPath,
                BranchId = req.branchId,
                DepartId = req.departmentId,
                Dscrp = req.pointName,
                StartWith = req.startWith,
                StorePath = unit.accountUnits.StorePath,
            };

            _context.PArcivingPoints.Add(point);
            await _context.SaveChangesAsync();

            var response = new ArchivingPointResponseDTOs
            {
                Id = point.Id,
                backupPath = point.BackupPath,
                accountUnitId = point.AccountUnitId,
                branchId = point.BranchId, 
                departmentId = point.DepartId,
                pointName = point.Dscrp,
                startWith = point.StartWith, 
                storePath = point.StorePath,
            };


            return (response, null); 
        }

        public async Task<(ArchivingPointResponseDTOs? point, string? error)> EditArchivingPoint(ArchivingPointViewForm req, int Id)
        {
            var point = await _context.PArcivingPoints.FirstOrDefaultAsync(e => e.Id == Id);
            if (point == null)
                return (null, "404"); // Archiving point not found

            // Check for duplicate name in another record
            var duplicate = await _context.PArcivingPoints
                .FirstOrDefaultAsync(e => e.Dscrp == req.pointName && e.Id != Id);
            if (duplicate != null)
                return (null, "400"); // Another archiving point with the same name exists

            var branch = await _infrastucatre.GetBranchById(req.branchId);
            if (branch.Branch == null)
                return (null, "404");

            var unit = await _infrastucatre.GetAccountUintById(req.accountUnitId);
            if (unit.accountUnits == null)
                return (null, "404");

            var depart = await _infrastucatre.GetDepartmentById(req.departmentId);
            if (depart.Department == null)
                return (null, "404");

            // Update properties
            point.AccountUnitId = req.accountUnitId;
            point.BranchId = req.branchId;
            point.DepartId = req.departmentId;
            point.Dscrp = req.pointName;
            point.StartWith = req.startWith;

            _context.PArcivingPoints.Update(point);
            await _context.SaveChangesAsync();

            var response = new ArchivingPointResponseDTOs
            {
                Id = point.Id,
                backupPath = point.BackupPath,
                accountUnitId = point.AccountUnitId,
                branchId = point.BranchId,
                departmentId = point.DepartId,
                pointName = point.Dscrp,
                startWith = point.StartWith,
                storePath = point.StorePath,
            };

            return (response, null);
        }
        public async Task<BaseResponseDTOs> GetArchivePointByDepartId(int departId)
        {
            var point = await _context.PArcivingPoints.FirstOrDefaultAsync(d => d.DepartId == departId);

            if (point == null)
                return new BaseResponseDTOs(null, 404, "there is not archivingPoint for this Depart");

            var result = new ArchivingPointResponseDTOs
            {
                Id = point.Id,
                backupPath = point.BackupPath,
                accountUnitId = point.AccountUnitId,
                branchId = point.BranchId,
                departmentId = point.DepartId,
                pointName = point.Dscrp,
                startWith = point.StartWith
            };
            return new BaseResponseDTOs(result, 200, "Welcome any help ;");
                
        }
        public async Task<(List<ArchivingPointResponseDTOs>? points, string? error)> GetAllArchivingPoints()
        {
            var points = await _context.PArcivingPoints.ToListAsync();
            if (points.Count == 0 || points == null)
                return (null, "404");//not found any archivingDocs

            return (points.Select(point => new ArchivingPointResponseDTOs
            {
                Id = point.Id,
                backupPath = point.BackupPath,
                accountUnitId = point.AccountUnitId,
                branchId = point.BranchId,
                departmentId = point.DepartId,
                pointName = point.Dscrp,
                startWith = point.StartWith,
                storePath = point.StorePath
            }).ToList(), null);
        }
        public async Task<(ArchivingPointResponseDTOs? point, string? error)> GetArchivingPointById(int Id)
        {
           var point = await _context.PArcivingPoints.FirstOrDefaultAsync(p => p.Id ==  Id);
            if (point == null)
                return (null, "404");// not found Archive Point

           return (new ArchivingPointResponseDTOs
            {
                Id = point.Id,
                backupPath = point.BackupPath,
                accountUnitId = point.AccountUnitId,
                branchId = point.BranchId,
                departmentId = point.DepartId,
                pointName = point.Dscrp,
                startWith = point.StartWith,
                storePath = point.StorePath
            }, null);
        }

        public async Task<string> DeleteArchivingPoint(int Id)
        {
           var point = await _context.PArcivingPoints.FindAsync(Id);
            if (point == null)
            {
                return "404"; // Not Archiving point Found
            }
            _context.PArcivingPoints.Remove(point);
            await _context.SaveChangesAsync();
            return "200"; // Success
        }

        //DocsType Logic Implementation
        public async Task<(DocTypeResponseDTOs? docsType, string? error)> PostDocsType(DocTypeViewform req)
        {
            var userId = await _systemInfoServices.GetUserId();
            if(userId.Id == null)
                return (null, "403"); // Unauthorized
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(u => u.UserId.ToString() == userId.Id);
            if(userPermissions.AddParameters == 0)
                return (null, "403"); // Forbidden

            var docsType = await _context.ArcivDocDscrps.FirstOrDefaultAsync(e => e.Dscrp == req.docuName && e.DepartId == req.departmentId);
            if(docsType != null)
                return (null, "400"); // Document type already exists

            docsType = new Models.Entity.ArcivDocDscrp
            {
                Dscrp = req.docuName,
                DepartId = req.departmentId,
                BranchId = req.branchId,
                AccountUnitId = req.AccountUnitId,
                IsoCode = req?.isCode
            };

            _context.ArcivDocDscrps.Add(docsType);
            await _context.SaveChangesAsync();

            var response = new DocTypeResponseDTOs
            {
                Id = docsType.Id,
                docuName = docsType.Dscrp,
                departmentId = docsType.DepartId ?? 0,
                branchId = docsType.BranchId ?? 0,
                AccountUnitId = docsType.AccountUnitId ?? 0,
                isCode = docsType?.IsoCode 
            };
            return (response, null);
        }
        public async Task<(DocTypeResponseDTOs? docsType, string? error)> EditDocsType(DocTypeViewform req, int Id)
        {
            var docsType = await _context.ArcivDocDscrps.FirstOrDefaultAsync(e => e.Id == Id);
            if (docsType == null)
                return (null, "404"); // Document type not found

            // Check for duplicate name (excluding current)
            var duplicate = await _context.ArcivDocDscrps
                .FirstOrDefaultAsync(e => e.Dscrp == req.docuName && e.Id != Id);
            if (duplicate != null)
                return (null, "400"); // Document type already exists

            docsType.Dscrp = req.docuName;
            docsType.DepartId = req.departmentId;
            docsType.BranchId = req.branchId;
            docsType.AccountUnitId = req.AccountUnitId;
            docsType.IsoCode = req.isCode;

            _context.ArcivDocDscrps.Update(docsType);
            await _context.SaveChangesAsync();

            var response = new DocTypeResponseDTOs
            {
                Id = docsType.Id,
                docuName = docsType.Dscrp,
                departmentId = docsType.DepartId ?? 0,
                branchId = docsType.BranchId ?? 0,
                AccountUnitId = docsType.AccountUnitId ?? 0,
                isCode = docsType.IsoCode
            };
            return (response, null);
        }
        public async Task<(List<DocTypeResponseDTOs>? docsTypes, string? error)> GetAllDocsTypes()
        {
            var docsTypes = await _context.ArcivDocDscrps.ToListAsync();
            if (docsTypes == null || docsTypes.Count == 0)
                return (null, "404"); // No document types found

            // Get all department IDs from the document types
            var departmentIds = docsTypes
                .Where(d => d.DepartId.HasValue)
                .Select(d => d.DepartId.Value)
                .Distinct()
                .ToList();

            // Load all relevant departments at once
            var departments = await _context.GpDepartments
                .Where(d => departmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d);

            var response = docsTypes.Select(d => new DocTypeResponseDTOs
            {
                Id = d.Id,
                docuName = d.Dscrp,
                departmentId = d.DepartId ?? 0,
                branchId = d.BranchId ?? 0,
                AccountUnitId = d.AccountUnitId ?? 0,
                isCode = d.IsoCode,
                // Add department name if available
                departmentName = d.DepartId.HasValue && departments.TryGetValue(d.DepartId.Value, out var dept)
                    ? dept.Dscrp
                    : null
            }).ToList();

            return (response, null);
        }

        public async Task<(DocTypeResponseDTOs? docsType, string? error)> GetDocsTypeById(int Id)
        {
            var docsType = await _context.ArcivDocDscrps.FirstOrDefaultAsync(e => e.Id == Id);
            if (docsType == null)
                return (null, "404"); // Document type not found

            var response = new DocTypeResponseDTOs
            {
                Id = docsType.Id,
                docuName = docsType.Dscrp,
                departmentId = docsType.DepartId ?? 0,
                branchId = docsType.BranchId ?? 0,
                AccountUnitId = docsType.AccountUnitId ?? 0,
                isCode = docsType.IsoCode
            };
            return (response, null);
        }

        public async Task<(List<DocTypeResponseDTOs>? docsType, string? error)> GetDocsTypeByDepartId(int DepartId)
        {
            var docsType = await _context.ArcivDocDscrps
                .Where(e => e.DepartId == DepartId)
                .Select(d => new DocTypeResponseDTOs
                {
                    Id = d.Id,
                    docuName = d.Dscrp,
                    departmentId = d.DepartId ?? 0,
                    AccountUnitId = d.AccountUnitId ?? 0,
                    branchId= d.BranchId ?? 0,
                    isCode= d.IsoCode
                })
                .ToListAsync();

            if (docsType == null || !docsType.Any())
                return (null, "404"); // Document type not found

            return (docsType, null);
        }

        public async Task<string> DeleteDocsType(int Id)
        {
            var point = await _context.ArcivDocDscrps.FindAsync(Id);
            if (point == null)
            {
                return "404"; // Not Archiving point Found
            }
            _context.ArcivDocDscrps.Remove(point);
            await _context.SaveChangesAsync();
            return "200"; // Success;
        }


        //SupDocsType Logic Implementation
        public async Task<(SupDocsTypeResponseDTOs? supDocsType, string? error)> PostSupDocsType(SupDocsTypeViewform req)
        {
            var userId = await _systemInfoServices.GetUserId();
            if (userId.Id == null)
                return (null, "401"); // Unauthorized
            var userPermissions = await _context.UsersOptionPermissions.FirstOrDefaultAsync(u => u.UserId.ToString() == userId.Id);
            if (userPermissions.AddParameters != 1)
                return (null, "403"); // Forbidden

            var sup = await _context.ArcivSubDocDscrps.FirstOrDefaultAsync(e => e.Dscrp == req.supDocuName);
            if(sup != null)
                return (null, "400"); // SupDocsType already exists

            var docId = await _context.ArcivDocDscrps.FindAsync(req.DocTypeId);
            if(docId == null)
                return (null, "404"); // Document type not found

            sup = new ArcivSubDocDscrp
            {
                Dscrp = req.supDocuName,
                DocTypeId = req.DocTypeId
            };

            _context.ArcivSubDocDscrps.Add(sup);
            await _context.SaveChangesAsync();
            var rseponse = new SupDocsTypeResponseDTOs
            {
                Id = sup.Id,
                supDocuName = sup.Dscrp,
                DocTypeId = sup?.DocTypeId
            };

            return (rseponse, null);    
        }
        public async Task<(SupDocsTypeResponseDTOs? supDocsType, string? error)> GetSupDocsTypeById(int Id)
        {
            var sup = await _context.ArcivSubDocDscrps.FirstOrDefaultAsync(e => e.Id == Id);
            if (sup == null)
                return (null, "404"); // SupDocsType not found

            var response = new SupDocsTypeResponseDTOs
            {
                Id = sup.Id,
                supDocuName = sup.Dscrp,
                DocTypeId = sup.DocTypeId
            };
            return (response, null);
        }


        public async Task<(List<SupDocsTypeResponseDTOs>? supDocsType, string? error)> GetSupDocsTypeByDocTypeId(int Id)
        {
            var sup = await _context.ArcivSubDocDscrps
                .Where(e => e.DocTypeId == Id)
                .Select(d => new SupDocsTypeResponseDTOs
                {
                    Id = d.Id,
                    supDocuName = d.Dscrp,
                    DocTypeId = d.DocTypeId
                })
                .ToListAsync();
            if (sup == null || !sup.Any())
                return (null, "404"); // SupDocsType not found

         
            return (sup, null);
        }

        public async Task<(List<SupDocsTypeResponseDTOs>? supDocsTypes, string? error)> GetAllSupDocsTypes()
        {
            var sups = await _context.ArcivSubDocDscrps.ToListAsync();
            if (sups == null || sups.Count == 0)
                return (null, "404"); // No SupDocsTypes found

            var response = sups.Select(s => new SupDocsTypeResponseDTOs
            {
                Id = s.Id,
                supDocuName = s.Dscrp,
                DocTypeId = s.DocTypeId
            }).ToList();

            return (response, null);
        }
        public async Task<(SupDocsTypeResponseDTOs? supDocsType, string? error)> EditSupDocsType(SupDocsTypeViewform req, int Id)
        {
            var sup = await _context.ArcivSubDocDscrps.FirstOrDefaultAsync(e => e.Id == Id);
            if (sup == null)
                return (null, "404"); // SupDocsType not found

            // Check for duplicate name (excluding current)
            var duplicate = await _context.ArcivSubDocDscrps
                .FirstOrDefaultAsync(e => e.Dscrp == req.supDocuName && e.Id != Id);
            if (duplicate != null)
                return (null, "400"); // SupDocsType already exists

            var docType = await _context.ArcivDocDscrps.FindAsync(req.DocTypeId);
            if (docType == null)
                return (null, "404"); // Document type not found

            sup.Dscrp = req.supDocuName;
            sup.DocTypeId = req.DocTypeId;

            _context.ArcivSubDocDscrps.Update(sup);
            await _context.SaveChangesAsync();

            var response = new SupDocsTypeResponseDTOs
            {
                Id = sup.Id,
                supDocuName = sup.Dscrp,
                DocTypeId = sup.DocTypeId
            };
            return (response, null);
        }

        public async Task<string> DeleteSupDocsType(int Id)
        {
            var sup = await _context.ArcivSubDocDscrps.FirstOrDefaultAsync(e => e.Id == Id);
            if (sup == null)
                return "404"; // SupDocsType not found

            _context.ArcivSubDocDscrps.Remove(sup);
            await _context.SaveChangesAsync();
            return "200"; // Success
        }

        //Precednce Logic Implementation
        public async Task<(PrecedenceResponseDTOs? precednce, string? error)> PostPrecednce(PrecedenceViewForm req)
        {
          var precednce =await  _context.WfPPrecedences.FirstOrDefaultAsync(e => e.Dscrp == req.precedenceName);
            if (precednce != null)
                return (null, "400"); // Precedence already exists
            var newPrecedence = new WfPPrecedence
            {
                Dscrp = req.precedenceName,
                MaxDate = req.maxDate
            };
            _context.WfPPrecedences.Add(newPrecedence);
            await _context.SaveChangesAsync();
            var response = new PrecedenceResponseDTOs
            {
                Id = newPrecedence.Id,
                precedenceName = newPrecedence.Dscrp,
                MaxDate = newPrecedence.MaxDate
            };
            return (response, null);
        }

        public async Task<(List<PrecedenceResponseDTOs>? precednces, string? error)> GetAllPrecednces()
        {
            var precednces = await _context.WfPPrecedences.ToListAsync();
            if (precednces == null || precednces.Count == 0)
                return (null, "404"); // No precedences found

            var response = precednces.Select(p => new PrecedenceResponseDTOs
            {
                Id = p.Id,
                precedenceName = p.Dscrp,
                MaxDate = p.MaxDate
            }).ToList();

            return (response, null);
        }
        public async Task<(PrecedenceResponseDTOs? precednce, string? error)> EditPrecednce(PrecedenceViewForm req, int Id)
        {
            var precednce = await _context.WfPPrecedences.FirstOrDefaultAsync(e => e.Id == Id);
            if (precednce == null)
                return (null, "404"); // Precedence not found

            // Check for duplicate name (excluding current)
            var duplicate = await _context.WfPPrecedences
                .FirstOrDefaultAsync(e => e.Dscrp == req.precedenceName && e.Id != Id);
            if (duplicate != null)
                return (null, "400"); // Precedence already exists

            precednce.Dscrp = req.precedenceName;
            precednce.MaxDate = req.maxDate;

            _context.WfPPrecedences.Update(precednce);
            await _context.SaveChangesAsync();

            var response = new PrecedenceResponseDTOs
            {
                Id = precednce.Id,
                precedenceName = precednce.Dscrp,
                MaxDate = precednce.MaxDate
            };
            return (response, null);
        }
        public async Task<(PrecedenceResponseDTOs? precednce, string? error)> GetPrecednceById(int Id)
        {
            var precednce = await _context.WfPPrecedences.FirstOrDefaultAsync(e => e.Id == Id);
            if (precednce == null)
                return (null, "404"); // Precedence not found

            var response = new PrecedenceResponseDTOs
            {
                Id = precednce.Id,
                precedenceName = precednce.Dscrp,
                MaxDate = precednce.MaxDate
            };
            return (response, null);
        }
        public async Task<string> DeletePrecednce(int Id)
        {
            var precednce = await _context.WfPPrecedences.FirstOrDefaultAsync(e => e.Id == Id);
            if (precednce == null)
                return "404"; // Precedence not found

            _context.WfPPrecedences.Remove(precednce);
            await _context.SaveChangesAsync();
            return "200"; // Success
        }
        /// create filters for all the implmention
        /// 
        // Add this method at the top of the ArchivingSettingsServices class
        private IQueryable<T> ApplyFilters<T>(IQueryable<T> query, object filters) where T : class
        {
            if (filters == null)
                return query;

            // Get all properties of the filter object that have values
            var filterProperties = filters.GetType().GetProperties()
                .Where(p => p.GetValue(filters) != null)
                .ToList();

            foreach (var prop in filterProperties)
            {
                var value = prop.GetValue(filters);
                if (value == null)
                    continue;

                // Skip empty strings and default values
                if ((value is string strValue && string.IsNullOrWhiteSpace(strValue)) ||
                    (value is int intValue && intValue == 0))
                    continue;

                // Map property names from filter object to entity properties
                var entityPropName = GetEntityPropertyName(typeof(T), prop.Name);
                if (entityPropName == null)
                    continue;

                // Check if the entity has this property
                var entityProperty = typeof(T).GetProperty(entityPropName);
                if (entityProperty == null)
                    continue;

                // Build dynamic expression for filtering
                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
                var property = System.Linq.Expressions.Expression.Property(parameter, entityPropName);

                System.Linq.Expressions.Expression condition;

                if (value is string)
                {
                    // For string properties, use case-insensitive Contains
                    var methodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var constant = System.Linq.Expressions.Expression.Constant(value.ToString());

                    condition = System.Linq.Expressions.Expression.Call(property, methodInfo, constant);
                }
                else
                {
                    // For numeric and other properties, use equality
                    var targetType = entityProperty.PropertyType;
                    var convertedValue = value;
                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        convertedValue = Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType));
                        var constant = System.Linq.Expressions.Expression.Constant(convertedValue, targetType);
                        condition = System.Linq.Expressions.Expression.Equal(property, constant);
                    }
                    else
                    {
                        var constant = System.Linq.Expressions.Expression.Constant(value);
                        condition = System.Linq.Expressions.Expression.Equal(property, constant);
                    }
                }

                var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(condition, parameter);
                query = query.Where(lambda);
            }

            return query;
        }
        
        // Helper method to map filter property names to entity property names
        private string GetEntityPropertyName(Type entityType, string filterPropertyName)
        {
            // Common mappings between filter properties and entity properties
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // PArcivingPoint mappings
                {"pointName", "Dscrp"},
                {"departmentId", "DepartId"},
                {"branchId", "BranchId"},
                {"accountUnitId", "AccountUnitId"},
                {"startWith", "StartWith"},
        
                // ArcivDocDscrp mappings
                {"docuName", "Dscrp"},
                {"isCode", "IsoCode"},
        
                // ArcivSubDocDscrp mappings
                {"supDocuName", "Dscrp"},
                {"DocTypeId", "DocTypeId"},
        
                // WfPPrecedence mappings
                {"precedenceName", "Dscrp"},
                {"maxDate", "MaxDate"}
            };

            // If we have a direct mapping, use it
            if (mappings.TryGetValue(filterPropertyName, out var mappedName))
                return mappedName;

            // Otherwise, check if the entity has a property with the same name
            if (entityType.GetProperty(filterPropertyName) != null)
                return filterPropertyName;

            // If no match found, return null
            return null;
        }

        // Updated GetAllArchivingPoints method with filtering and BaseResponseDTOs
        public async Task<BaseResponseDTOs> GetAllArchivingPoints(ArchivingPointViewForm? filters = null)
        {
            var query = _context.PArcivingPoints.AsQueryable();

            // Apply filters if provided
            if (filters != null)
            {
                query = ApplyFilters(query, filters);
            }

            var points = await query.ToListAsync();
            if (points == null || points.Count == 0)
                return new BaseResponseDTOs(null, 404, "No archiving points found");

            var result = points.Select(point => new ArchivingPointResponseDTOs
            {
                Id = point.Id,
                backupPath = point.BackupPath,
                accountUnitId = point.AccountUnitId,
                branchId = point.BranchId,
                departmentId = point.DepartId,
                pointName = point.Dscrp,
                startWith = point.StartWith,
                storePath = point.StorePath
            }).ToList();

            return new BaseResponseDTOs(result, 200);
        }

        // Updated GetAllDocsTypes method with filtering and BaseResponseDTOs
        public async Task<BaseResponseDTOs> GetAllDocsTypes(DocTypeViewform? filters = null)
        {
            var query = _context.ArcivDocDscrps.AsQueryable();

            // Apply filters if provided
            if (filters != null)
            {
                query = ApplyFilters(query, filters);
            }

            var docsTypes = await query.ToListAsync();
            if (docsTypes == null || docsTypes.Count == 0)
                return new BaseResponseDTOs(null, 404, "No document types found");

            // Get all department IDs from the document types
            var departmentIds = docsTypes
                .Where(d => d.DepartId.HasValue)
                .Select(d => d.DepartId.Value)
                .Distinct()
                .ToList();

            // Load all relevant departments at once
            var departments = await _context.GpDepartments
                .Where(d => departmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d);

            var result = docsTypes.Select(d => new DocTypeResponseDTOs
            {
                Id = d.Id,
                docuName = d.Dscrp,
                departmentId = d.DepartId ?? 0,
                branchId = d.BranchId ?? 0,
                AccountUnitId = d.AccountUnitId ?? 0,
                isCode = d.IsoCode,
                // Add department name if available
                departmentName = d.DepartId.HasValue && departments.TryGetValue(d.DepartId.Value, out var dept)
                    ? dept.Dscrp
                    : null
            }).ToList();

            return new BaseResponseDTOs(result, 200);
        }

        // Updated GetAllSupDocsTypes method with filtering and BaseResponseDTOs
        public async Task<BaseResponseDTOs> GetAllSupDocsTypes(SupDocsTypeViewform? filters = null)
        {
            var query = _context.ArcivSubDocDscrps.AsQueryable();

            // Apply filters if provided
            if (filters != null)
            {
                query = ApplyFilters(query, filters);
            }

            var sups = await query.ToListAsync();
            if (sups == null || sups.Count == 0)
                return new BaseResponseDTOs(null, 404, "No supplementary document types found");

            var result = sups.Select(s => new SupDocsTypeResponseDTOs
            {
                Id = s.Id,
                supDocuName = s.Dscrp,
                DocTypeId = s.DocTypeId
            }).ToList();

            return new BaseResponseDTOs(result, 200);
        }

        // Updated GetAllPrecednces method with filtering and BaseResponseDTOs
        public async Task<BaseResponseDTOs> GetAllPrecednces(PrecedenceViewForm? filters = null)
        {
            var query = _context.WfPPrecedences.AsQueryable();

            // Apply filters if provided
            if (filters != null)
            {
                query = ApplyFilters(query, filters);
            }

            var precednces = await query.ToListAsync();
            if (precednces == null || precednces.Count == 0)
                return new BaseResponseDTOs(null, 404, "No precedences found");

            var result = precednces.Select(p => new PrecedenceResponseDTOs
            {
                Id = p.Id,
                precedenceName = p.Dscrp,
                MaxDate = p.MaxDate
            }).ToList();

            return new BaseResponseDTOs(result, 200);
        }
    }
}
