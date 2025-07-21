using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;

namespace Nastya_Archiving_project.Services.Permmsions
{
    public class PermissionsServices : BaseServices,IPermissionsServices
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
    }
}
