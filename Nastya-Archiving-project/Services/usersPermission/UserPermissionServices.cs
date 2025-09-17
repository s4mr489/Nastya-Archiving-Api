using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Infrastruture.Derpatment;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Models.DTOs.UserPermission;
using Nastya_Archiving_project.Services.auth;
using Nastya_Archiving_project.Services.encrpytion;

namespace Nastya_Archiving_project.Services.usersPermission
{
    public class UserPermissionServices : BaseServices, IUserPermissionsServices
    {
        private readonly AppDbContext _context;
        private readonly IEncryptionServices _encryptionServices;
        private readonly IAuthServices _authServices;

        public UserPermissionServices(AppDbContext context , IEncryptionServices encryptionServices ) : base(null , context)
        {
            _context = context;
            _encryptionServices = encryptionServices;
        }
        //that method used to get the user based on the accountUnitId and the DepartMintId And the BranchId
        public async Task<List<UsersSearchResponseDTOs>> GetUsersAsync(UsersSearchViewForm search)
        {
            var query = _context.Users.AsQueryable();

            // Apply all non-encrypted filters
            if (search.accountUnitId.HasValue)
                query = query.Where(d => d.AccountUnitId == search.accountUnitId.Value);
            if (search.branchId.HasValue)
                query = query.Where(d => d.BranchId == search.branchId.Value);
            if (search.departmentId.HasValue)
                query = query.Where(d => d.DepariId == search.departmentId.Value);

            // Skip the realname filter for now (we'll apply it after decryption)

            // Get all users based on the other filters
            var users = await query.OrderByDescending(d => d.Id).ToListAsync();

            // Now decrypt and filter by realname if needed
            if (!string.IsNullOrEmpty(search.userRealName))
            {
                users = users.Where(u =>
                    u.Realname != null &&
                    _encryptionServices.DecryptString256Bit(u.Realname)
                        .Contains(search.userRealName, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Apply pagination after filtering
            int pageNumber = search.pageNumber != null && search.pageNumber > 0 ? search.pageNumber.Value : 1;
            int pageSize = search.pageSize != null && search.pageSize > 0 ? search.pageSize.Value : 20;

            users = users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            if (users == null || users.Count == 0)
            {
                return new List<UsersSearchResponseDTOs>();
            }

            var result = users.Select(u => new UsersSearchResponseDTOs
            {
                userId = u.Id,
                realName = u.Realname != null ? _encryptionServices.DecryptString256Bit(u.Realname) : null,
                Activation = u.Stoped,
                JoinDate = u.EditDate.HasValue
                    ? new DateTime(u.EditDate.Value.Year, u.EditDate.Value.Month, u.EditDate.Value.Day)
                    : DateTime.MinValue,

                // Map other properties as needed
            }).ToList();

            return result;
        }


        //this method is used to get user permissions based on the user Id provided then 
        public async Task<BaseResponseDTOs> GetUserPermissionsAsync(UsersViewForm users)
        {
            var userIds = new List<int>();
            if (users.Id.HasValue)
            {
                userIds.Add(users.Id.Value);
            }

            // Error handling: check if any required data is missing
            if (!userIds.Any())
                return new BaseResponseDTOs(null, 404, "User ID not provided.");

            // Query for user permissions
            var optionPermissions = await _context.UsersOptionPermissions
                .Where(p => userIds.Contains(p.UserId ?? 0))
                .ToListAsync();

            var archivingPoints = await _context.UsersArchivingPointsPermissions
                .Where(p => userIds.Contains(p.UserId ?? 0))
                .ToListAsync();

            // If no permissions found, still continue (as per your commented code)

            // Get archiving point IDs for further lookups
            var archivingPointIds = archivingPoints
                .Select(p => p.ArchivingpointId ?? 0)
                .Where(id => id > 0)
                .ToList();
            var userActivation = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .FirstOrDefaultAsync();
            // Get archiving point details
            var archivingPointNames = await _context.PArcivingPoints
                .Where(a => archivingPointIds.Contains(a.Id))
                .ToListAsync();

            // Get file types
            var asaWfuser = await _context.PFileTypes
                .Where(a => archivingPointIds.Contains(a.Id))
                .ToListAsync();

            // Get department IDs from both UsersArchivingPointsPermission and PArcivingPoints
            var departmentIds = new HashSet<int>();

            // Add department IDs from permissions
            foreach (var point in archivingPoints)
            {
                if (point.DepartId.HasValue && point.DepartId.Value > 0)
                {
                    departmentIds.Add(point.DepartId.Value);
                }
            }

            // Add department IDs from archiving points as fallback
            foreach (var archPoint in archivingPointNames)
            {
                if (archPoint.DepartId.HasValue && archPoint.DepartId.Value > 0)
                {
                    departmentIds.Add(archPoint.DepartId.Value);
                }
            }

            // Get department details
            var departments = await _context.GpDepartments
                .Where(d => departmentIds.Contains(d.Id))
                .ToListAsync();

            // Create the response
            var result = new List<UsersSearchResponseDTOs>
            {
                new UsersSearchResponseDTOs
                {
                    userId = users.Id ?? 0,
                    username = _encryptionServices.DecryptString256Bit(_context.Users.FirstOrDefault(u => u.Id == (users.Id ?? 0))?.UserName),
                    realName = _encryptionServices.DecryptString256Bit(_context.Users.FirstOrDefault(u => u.Id == (users.Id ?? 0))?.Realname ?? string.Empty),
                    fileType = asaWfuser.FirstOrDefault(a => a.Id == (users.Id ?? 0))?.Id,
                    usersOptionPermission = optionPermissions.FirstOrDefault(p => p.UserId == users.Id),
                    Activation = userActivation.Stoped,
                    // Convert DateOnly? to DateTime? (if needed) - this is the fix for the error
                        JoinDate = userActivation.EditDate.HasValue
                                ? new DateTime(userActivation.EditDate.Value.Year, userActivation.EditDate.Value.Month, userActivation.EditDate.Value.Day)
                                : DateTime.MinValue, //
                    archivingPoint = archivingPoints
                        .Where(p => p.UserId == users.Id)
                        .Select(p => new ArchivingPermissionResponseDTOs
                        {
                            archivingPointId = p.ArchivingpointId ?? 0,
                            archivingPointDscrp = archivingPointNames
                                .FirstOrDefault(a => a.Id == p.ArchivingpointId)?.Dscrp,
                            departId = p.DepartId ?? archivingPointNames
                                .FirstOrDefault(a => a.Id == p.ArchivingpointId)?.DepartId ?? 0,
                            departmentName = GetDepartmentName(p.DepartId, p.ArchivingpointId, archivingPointNames, departments)
                        })
                        .ToList(),
                }
            };

            return new BaseResponseDTOs(result, 200);
        }

        // Helper method to get department name with proper fallback logic
        private string? GetDepartmentName(
            int? departId,
            int? archivingPointId,
            List<PArcivingPoint> archivingPoints,
            List<GpDepartment> departments)
        {
            // If department ID is provided directly in the permission
            if (departId.HasValue && departId.Value > 0)
            {
                var department = departments.FirstOrDefault(d => d.Id == departId.Value);
                if (department != null)
                {
                    return department.Dscrp;
                }
            }

            // Fallback: Try to get department ID from archiving point
            if (archivingPointId.HasValue && archivingPointId.Value > 0)
            {
                var archivingPoint = archivingPoints.FirstOrDefault(a => a.Id == archivingPointId.Value);
                if (archivingPoint?.DepartId.HasValue == true)
                {
                    var department = departments.FirstOrDefault(d => d.Id == archivingPoint.DepartId.Value);
                    if (department != null)
                    {
                        return department.Dscrp;
                    }
                }
            }

            return null;
        }




        // Method to create (add) user permissions based on the userId and provided permission data
        public async Task<BaseResponseDTOs> CreateUserPermissionsAsync(CreateUserPermissionsRequestDTO request)
        {
            // Validate user existence
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return new BaseResponseDTOs(null, 404, "User not found.");

            // Check if option permission exists for this user
            var optionPermission = await _context.UsersOptionPermissions
                .FirstOrDefaultAsync(p => p.UserId == request.UserId);

            if (optionPermission != null)
            {
                // If exists, update the permission fields
                optionPermission.AddParameters = request.addParameters ?? optionPermission.AddParameters;
                optionPermission.AllowDelete = request.allowDelete ?? optionPermission.AllowDelete;
                optionPermission.AllowAddToOther = request.allowAddToOther ?? optionPermission.AllowDownload;
                optionPermission.AllowViewTheOther = request.allowViewTheOther;
                optionPermission.AllowSendMail = request.allowSendMail;
                optionPermission.AllowDownload = request.allowDownload;
                _context.UsersOptionPermissions.Update(optionPermission);
            }
            else
            {
                // If not exists, create new
                _context.UsersOptionPermissions.Add(new UsersOptionPermission
                {
                    UserId = request.UserId,
                    AddParameters = request.addParameters,
                    AllowDelete = request.allowDelete,
                    AllowAddToOther = request.allowAddToOther,
                    AllowViewTheOther = request.allowViewTheOther,
                    AllowSendMail = request.allowSendMail,
                    AllowDownload = request.allowDownload
                });
            }

            // STEP 1: Remove all existing department permissions for this user
            var existingDepartPermissions = await _context.UsersArchivingPointsPermissions
                .Where(p => p.UserId == request.UserId && p.DepartId != null)
                .ToListAsync();

            if (existingDepartPermissions.Any())
            {
                _context.UsersArchivingPointsPermissions.RemoveRange(existingDepartPermissions);
                // SaveChanges is not called here to batch all operations in a single transaction
            }

            // STEP 2: Add new department permissions
            if (request.DepartIds != null && request.DepartIds.Count > 0)
            {
                foreach (var departId in request.DepartIds)
                {
                    // Since we've removed all existing department permissions,
                    // we can directly add new ones without checking if they exist
                    _context.UsersArchivingPointsPermissions.Add(new UsersArchivingPointsPermission
                    {
                        UserId = request.UserId,
                        DepartId = departId
                    });
                }
            }

            // Add or update archiving points if provided
            if (request.ArchivingPointIds != null && request.ArchivingPointIds.Count > 0)
            {
                // Optional: You could also clear existing archiving point permissions first
                // if you want the same behavior for archiving points
                var existingArchivingPermissions = await _context.UsersArchivingPointsPermissions
                    .Where(p => p.UserId == request.UserId && p.ArchivingpointId != null)
                    .ToListAsync();

                if (existingArchivingPermissions.Any())
                {
                    _context.UsersArchivingPointsPermissions.RemoveRange(existingArchivingPermissions);
                }

                // Add new archiving point permissions
                foreach (var archivingPointId in request.ArchivingPointIds)
                {
                    _context.UsersArchivingPointsPermissions.Add(new UsersArchivingPointsPermission
                    {
                        UserId = request.UserId,
                        ArchivingpointId = archivingPointId
                    });
                }
            }

            // Assign file type if provided
            if (request.FileTypeId.HasValue)
            {
                user.AsWfuser = request.FileTypeId.Value;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            return new BaseResponseDTOs("Permissions and file type assigned successfully.", 201);
        }

        // Method to delete user permissions and archiving points based on the userId and provided permission data
        public async Task<BaseResponseDTOs> DeleteUserPermissionsAsync(CreateUserPermissionsRequestDTO request)
        {
            // Validate user existence
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return new BaseResponseDTOs(null, 404, "User not found.");

            // Update option permission fields to 0 (remove permission) if exists
            var optionPermission = await _context.UsersOptionPermissions
                .FirstOrDefaultAsync(p => p.UserId == request.UserId);
            if (optionPermission != null)
            {
                optionPermission.AddParameters = 0;
                optionPermission.AllowDelete = 0;
                optionPermission.AllowAddToOther = 0;
                optionPermission.AllowViewTheOther = 0;
                optionPermission.AllowSendMail = 0;
                optionPermission.AllowDownload = 0;
                _context.UsersOptionPermissions.Update(optionPermission);
            }

            // Remove archiving points if provided (delete the mapping, not update)
            if (request.ArchivingPointIds != null && request.ArchivingPointIds.Count > 0)
            {
                var archivingPoints = await _context.UsersArchivingPointsPermissions
                    .Where(p => p.UserId == request.UserId && request.ArchivingPointIds.Contains(p.ArchivingpointId ?? 0))
                    .ToListAsync();

                if (archivingPoints.Any())
                {
                    _context.UsersArchivingPointsPermissions.RemoveRange(archivingPoints);
                }
            }

            // Remove department permissions if provided
            if (request.DepartIds != null && request.DepartIds.Count > 0)
            {
                var departPermissions = await _context.UsersArchivingPointsPermissions
                    .Where(p => p.UserId == request.UserId && request.DepartIds.Contains(p.DepartId ?? 0))
                    .ToListAsync();

                if (departPermissions.Any())
                {
                    _context.UsersArchivingPointsPermissions.RemoveRange(departPermissions);
                }
            }

            // Remove file type if requested
            if (request.FileTypeId.HasValue)
            {
                user.AsWfuser = null;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            return new BaseResponseDTOs("Permissions updated and file type deleted successfully.", 200);
        }

        public async Task<BaseResponseDTOs> GetAllPermissionsAndInfoForUser(int Id)
        {
           var user =await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if(user == null)
            {
                return new BaseResponseDTOs(null, 404, "User not found.");
            }
            var userInfo = new UserPermissionAndInfosResponseDTOs
            {
                Id = user.Id,
                username = user.UserName != null ? _encryptionServices.DecryptString256Bit(user.UserName) : null,
                RealName = user.Realname != null ? _encryptionServices.DecryptString256Bit(user.Realname) : null,
                Email = user.Email != null ? user.Email : null,
                PhoneNumber = user.PhoneNo != null ? user.PhoneNo : null,
                Address = user.Address != null ? user.Address : null,
                IsActive = user.Stoped == 1 ? "Stopped" : "Active",
                usersOptionPermissions = await _context.UsersOptionPermissions
                    .Where(p => p.UserId == Id)
                    .ToListAsync(),
                UserDepartement = await (from ud in _context.UsersArchivingPointsPermissions
                                        join d in _context.GpDepartments on ud.DepartId equals d.Id
                                        where ud.UserId == Id && ud.DepartId != null
                                        select new DepartmentResponseDTOs
                                        {
                                            Id = d.Id,
                                            DepartmentName = d.Dscrp
                                        }).Distinct().ToListAsync()
            };

            return new BaseResponseDTOs(userInfo, 200);
        }
    }

}
