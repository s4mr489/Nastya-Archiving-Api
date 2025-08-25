using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Models.DTOs.UserPermission;
using Nastya_Archiving_project.Services.encrpytion;

namespace Nastya_Archiving_project.Services.usersPermission
{
    public class UserPermissionServices : BaseServices, IUserPermissionsServices
    {
        private readonly AppDbContext _context;
        private readonly IEncryptionServices _encryptionServices;

        public UserPermissionServices(AppDbContext context , IEncryptionServices encryptionServices ) : base(null , context)
        {
            _context = context;
            _encryptionServices = encryptionServices;
        }
        //that method used to get the user based on the accountUnitId and the DepartMintId And the BranchId
        public async Task<List<UsersSearchResponseDTOs>> GetUsersAsync(UsersSearchViewForm search)
        {
            var query = _context.Users.AsQueryable();

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

            if (users == null || users.Count == 0)
            {
                // Option 1: Return an empty list (recommended for list endpoints)
                return new List<UsersSearchResponseDTOs>();
                // Option 2: Throw an exception or handle as needed
                // throw new Exception("No users found matching the criteria.");
            }

            var result = users.Select(u => new UsersSearchResponseDTOs
            {
                userId = u.Id,
                realName = _encryptionServices.DecryptString256Bit(u.Realname),
                // Map other properties as needed
            }).ToList();

            return result;
        }


        //this method is used to get user permissions based on the user Id provided then 
        //search on the same id on the UserOpetionPermission Like list 
        //search on the same id on the UsersArchivingPointsPermssions
        //search on the same object using the arhivingPoints  on the archivingPoint 
        public async Task<BaseResponseDTOs> GetUserPermissionsAsync(UsersViewForm users)
        {
            var userIds = new List<int>();
            if (users.Id.HasValue)
            {
                userIds.Add(users.Id.Value);
            }

            //Quersy Operation 
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

            var AsaWfuser = await _context.PFileTypes
                .Where(a => archivingPointIds.Contains(a.Id))
                .ToListAsync();

            // Error handling: check if any required data is missing
            if (!userIds.Any())
                return new BaseResponseDTOs(null, 404, "User ID not provided.");

            //if (!optionPermissions.Any() && !archivingPoints.Any())
            //    return new BaseResponseDTOs(null, 404, "No permissions found for the user.");

            //if (!archivingPointNames.Any())
            //    return new BaseResponseDTOs(null, 404, "No archiving points found for the user.");
            //cusotm response 
            var result = new List<UsersSearchResponseDTOs>
                {
                    new UsersSearchResponseDTOs
                    {
                        userId = users.Id ?? 0,
                        fileType = AsaWfuser.FirstOrDefault(a => a.Id == (users.Id ?? 0))?.Id,
                        usersOptionPermission = optionPermissions.FirstOrDefault(p => p.UserId == users.Id),
                        archivingPoint = archivingPoints
                        .Where(p => p.UserId == users.Id)
                        .Select(p => new ArchivingPermissionResponseDTOs
                        {
                            archivingPointId = p.ArchivingpointId ?? 0,
                            archivingPointDscrp = archivingPointNames
                                .Where(a => a.Id == p.ArchivingpointId)
                                .Select(a => a.Dscrp)
                                .FirstOrDefault()
                        })
                        .ToList(),
                    }
                };

            return new BaseResponseDTOs(result, 200);
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

            // Add or update archiving points if provided
            if (request.ArchivingPointIds != null && request.ArchivingPointIds.Count > 0)
            {
                foreach (var archivingPointId in request.ArchivingPointIds)
                {
                    var archivingPermission = await _context.UsersArchivingPointsPermissions
                        .FirstOrDefaultAsync(p => p.UserId == request.UserId && p.ArchivingpointId == archivingPointId);

                    if (archivingPermission == null)
                    {
                        // Add new if not exists
                        _context.UsersArchivingPointsPermissions.Add(new UsersArchivingPointsPermission
                        {
                            UserId = request.UserId,
                            ArchivingpointId = archivingPointId
                        });
                    }
                    // If exists, do nothing (no update logic for archiving point permissions in your model)
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

            // Remove file type if requested
            if (request.FileTypeId.HasValue)
            {
                user.AsWfuser = null;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            return new BaseResponseDTOs("Permissions updated and file type deleted successfully.", 200);
        }
    }
}
