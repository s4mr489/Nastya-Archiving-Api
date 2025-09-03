using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;

namespace Nastya_Archiving_project.Services.Permmsions
{
    public class PermissionsServices : BaseServices, IPermissionsServices
    {
        private readonly AppDbContext _context;
        public PermissionsServices(AppDbContext context) : base(null, context)
        {
            _context = context;
        }
        public async Task<string> AddGroupPermissions(int groupId, List<int> permissionsIds)
        {
            var group = await _context.Usersgroups.FindAsync(groupId);
            if (group == null)
                return "404"; // group Id Not Found

            var permissions = await _context.Usersinterfaces
                .Where(p => permissionsIds.Contains(p.Id))
                .ToListAsync();

            if (permissions.Count == 0)
                return "404"; // No valid Permissions Ids found

            foreach (var permission in permissions)
            {
                var groupPermission = new Userspermission
                {
                    Pageid = permission.Id.ToString(),
                    Groupid = group.groupid
                };
                _context.Userspermissions.Add(groupPermission);
            }

            await _context.SaveChangesAsync();
            return "200"; // Successfully added permissions
        }
        public async Task<string> DeletedGroupPermissions(int groupId, List<int> permissionsIds)
        {
            var group = await _context.Usersgroups.FindAsync(groupId);
            if (group == null)
                return "404"; // GroupId Not Found

            // Fetch all permissions for the group, then filter in memory
            var permissions = await _context.Userspermissions
                .Where(p => p.Groupid == group.groupid)
                .ToListAsync();

            var permissionsToRemove = permissions
                .Where(p => p.Pageid != null && permissionsIds.Contains(int.Parse(p.Pageid)))
                .ToList();

            if (permissionsToRemove.Count == 0)
                return "404"; //PermissionsId  Not Found

            _context.Userspermissions.RemoveRange(permissionsToRemove);
            await _context.SaveChangesAsync();
            return "200"; // Successfully deleted permissions
        }
        public async Task<(List<string>? permmsion, string? error)> GetPermissionsByGroupId(int groupId)
        {
            var group = await _context.Usersgroups.FindAsync(groupId);
            if (group == null)
                return (null, "404"); // GroupId Not Found
            var permissions = await _context.Userspermissions
                .Where(p => p.Groupid == group.groupid && p.Pageid != null)
                .Select(p => p.Pageid)
                .ToListAsync();
            if (permissions.Count == 0)
                return (null, "404"); // No permissions found for this group
            return (permissions, null);
        }

        public async Task<BaseResponseDTOs> GetAllPermissions()
        {
            var permission = await _context.UsersOptionPermissions.ToListAsync();
            if (permission == null)
                return new BaseResponseDTOs(null, 400, "no permission founded");
            return new BaseResponseDTOs(permission, 200, null);
        }

        public async Task<BaseResponseDTOs> CopyUserPermissionForGroup(int Id)
        {
            var permissions = await _context.UsersOptionPermissions
                .Where(p => p.UserId == Id)
                .Select(permission => new
                {
                    permission.Id,
                    permission.UserId,
                    permission.AddParameters,
                    permission.AllowDelete,
                    permission.AllowDownload,
                    permission.AllowAddToOther,
                    permission.AllowViewTheOther,
                    permission.AllowSendMail,
                })
                .ToListAsync();

            if (permissions == null || !permissions.Any())
                return new BaseResponseDTOs(null, 400, "No user permissions found");

            // Get the user associated with these permissions
            var userId = await _context.Users
                .Where(u => u.Id == Id)
                .FirstOrDefaultAsync();

            if (userId == null)
                return new BaseResponseDTOs(null, 404, "User not found");

            var groupedUsers = await _context.Users.Where(u => u.GroupId == userId.GroupId)
                .ToListAsync();

            foreach(var user in groupedUsers)
            {
                foreach (var permission in permissions)
                {
                    var existingPermission = await _context.Usersgroups
                        .FirstOrDefaultAsync(p => p.groupid == user.GroupId);
                    existingPermission.AllowDelete = permission.AllowDelete;
                    existingPermission.AddParameters = permission.AddParameters;
                    existingPermission.AllowDownload = permission.AllowDownload;
                    existingPermission.AllowAddToOther = permission.AllowAddToOther;
                    existingPermission.AllowSendMail = permission.AllowSendMail;
                    existingPermission.AllowViewTheOther = permission.AllowViewTheOther;
                    await _context.SaveChangesAsync();
                }
            }
            
            return new BaseResponseDTOs(permissions, 200, null);
        }


        public async Task<BaseResponseDTOs> CopyUserPermissionToMultipleUsers(int sourceUserId, List<int> targetUserIds)
        {
            // Get permissions from the source user
            var permissions = await _context.UsersOptionPermissions
                .Where(p => p.UserId == sourceUserId)
                .ToListAsync();

            if (permissions == null || !permissions.Any())
                return new BaseResponseDTOs(null, 400, "No permissions found for source user");

            // Check if source user exists
            var sourceUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == sourceUserId);

            if (sourceUser == null)
                return new BaseResponseDTOs(null, 404, "Source user not found");

            // Track successful and failed copies
            var results = new List<object>();
            int successCount = 0;

            foreach (var targetUserId in targetUserIds)
            {
                // Skip if target is the same as source
                if (targetUserId == sourceUserId)
                {
                    results.Add(new { UserId = targetUserId, Status = "Skipped (same as source)" });
                    continue;
                }

                // Check if target user exists
                var targetUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == targetUserId);

                if (targetUser == null)
                {
                    results.Add(new { UserId = targetUserId, Status = "Failed (user not found)" });
                    continue;
                }

                try
                {
                    // Check if user already has a permission record
                    var existingPermission = await _context.UsersOptionPermissions
                        .FirstOrDefaultAsync(p => p.UserId == targetUserId);

                    if (existingPermission != null)
                    {
                        // Update existing permission with values from source
                        existingPermission.AddParameters = permissions.First().AddParameters;
                        existingPermission.AllowDelete = permissions.First().AllowDelete;
                        existingPermission.AllowDownload = permissions.First().AllowDownload;
                        existingPermission.AllowAddToOther = permissions.First().AllowAddToOther;
                        existingPermission.AllowSendMail = permissions.First().AllowSendMail;
                        existingPermission.AllowViewTheOther = permissions.First().AllowViewTheOther;
                    }
                    else
                    {
                        // Create new permission only if one doesn't exist
                        var newPermission = new UsersOptionPermission
                        {
                            UserId = targetUserId,
                            AddParameters = permissions.First().AddParameters,
                            AllowDelete = permissions.First().AllowDelete,
                            AllowDownload = permissions.First().AllowDownload,
                            AllowAddToOther = permissions.First().AllowAddToOther,
                            AllowSendMail = permissions.First().AllowSendMail,
                            AllowViewTheOther = permissions.First().AllowViewTheOther
                        };
                        _context.UsersOptionPermissions.Add(newPermission);
                    }

                    await _context.SaveChangesAsync();
                    successCount++;
                    results.Add(new { UserId = targetUserId, Status = "Success" });
                }
                catch (Exception ex)
                {
                    results.Add(new { UserId = targetUserId, Status = $"Failed: {ex.Message}" });
                }
            }

            return new BaseResponseDTOs(new
            {
                SourceUserId = sourceUserId,
                TargetUsers = results,
                SuccessCount = successCount,
                TotalAttempted = targetUserIds.Count
            }, 200, successCount == 0 ? "Failed to copy permissions to any users" : null);
        }

        /// <summary>
        /// Copies permissions from a group to all users belonging to that group
        /// </summary>
        /// <param name="groupId">The ID of the group whose permissions will be copied to its users</param>
        /// <returns>A response indicating success or failure of the operation</returns>
        public async Task<BaseResponseDTOs> CopyGroupPermissionsToUsersAsync(int groupId)
        {
            // Step 1: Validate the group exists
            var group = await _context.Usersgroups.FindAsync(groupId);
            if (group == null)
                return new BaseResponseDTOs(null, 404, "Group not found.");

            // Step 2: Get all users belonging to this group
            var usersInGroup = await _context.Users
                .Where(u => u.GroupId == groupId)
                .ToListAsync();

            if (!usersInGroup.Any())
                return new BaseResponseDTOs(null, 404, "No users found in this group.");

            int successCount = 0;
            List<string> failedUsers = new List<string>();

            // Step 3: Process each user in the group
            foreach (var user in usersInGroup)
            {
                try
                {
                    // Step 3.1: Update or create UsersOptionPermission for the user
                    var optionPermission = await _context.UsersOptionPermissions
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);

                    if (optionPermission != null)
                    {
                        // Update existing permission
                        optionPermission.AddParameters = group.AddParameters;
                        optionPermission.AllowDelete = group.AllowDelete;
                        optionPermission.AllowAddToOther = group.AllowAddToOther;
                        optionPermission.AllowViewTheOther = group.AllowViewTheOther;
                        optionPermission.AllowSendMail = group.AllowSendMail;
                        optionPermission.AllowDownload = group.AllowDownload;
                        _context.UsersOptionPermissions.Update(optionPermission);
                    }
                    else
                    {
                        // Create new permission
                        _context.UsersOptionPermissions.Add(new UsersOptionPermission
                        {
                            UserId = user.Id,
                            AddParameters = group.AddParameters,
                            AllowDelete = group.AllowDelete,
                            AllowAddToOther = group.AllowAddToOther,
                            AllowViewTheOther = group.AllowViewTheOther,
                            AllowSendMail = group.AllowSendMail,
                            AllowDownload = group.AllowDownload
                        });
                    }

                    // Step 3.2: Copy group's archiving points and department permissions
                    // For this, we need to find if the group has any specific archiving points or departments
                    // Since the direct relation is not visible in the model, we'll use a representative user from the group

                    // Find a representative user who already has permissions (if any)
                    var representativeUser = await _context.Users
                        .Where(u => u.GroupId == groupId && u.Id != user.Id)
                        .FirstOrDefaultAsync();

                    if (representativeUser != null)
                    {
                        // Get the representative user's archiving points and department permissions
                        var representativePermissions = await _context.UsersArchivingPointsPermissions
                            .Where(p => p.UserId == representativeUser.Id)
                            .ToListAsync();

                        if (representativePermissions.Any())
                        {
                            // First, remove existing permissions for the current user
                            var existingPermissions = await _context.UsersArchivingPointsPermissions
                                .Where(p => p.UserId == user.Id)
                                .ToListAsync();

                            if (existingPermissions.Any())
                            {
                                _context.UsersArchivingPointsPermissions.RemoveRange(existingPermissions);
                            }

                            // Then add new permissions based on the representative user
                            foreach (var permission in representativePermissions)
                            {
                                _context.UsersArchivingPointsPermissions.Add(new UsersArchivingPointsPermission
                                {
                                    UserId = user.Id,
                                    ArchivingpointId = permission.ArchivingpointId,
                                    DepartId = permission.DepartId,
                                    AccountUnitId = permission.AccountUnitId
                                });
                            }
                        }
                    }

                    successCount++;
                }
                catch (Exception ex)
                {
                    // Log the error
                    failedUsers.Add($"(ID: {user.Id}): {ex.Message}");
                }
            }

            // Save all changes in a single transaction
            await _context.SaveChangesAsync();

            // Prepare the response message
            string message = $"Successfully copied permissions to {successCount} out of {usersInGroup.Count} users.";
            if (failedUsers.Any())
            {
                message += " Failed for users: " + string.Join(", ", failedUsers);
            }

            return new BaseResponseDTOs(message, 200);
        }
    }
}
