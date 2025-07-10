using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using System.Security.Claims;

namespace Nastya_Archiving_project.Services.SystemInfo
{
    public class SystemInfoServices : BaseServices , ISystemInfoServices
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        public SystemInfoServices(AppDbContext context, IHttpContextAccessor httpContext) : base(null, context)
        {
            _context = context;
            _httpContext = httpContext;
        }

        public async Task<string> GetLastRefNo()
        {
            var lastDoc = await _context.ArcivingDocs
                 .OrderByDescending(d => d.Id)
                 .FirstOrDefaultAsync();

            int lastNumber = 0;
            if (lastDoc != null && !string.IsNullOrEmpty(lastDoc.RefrenceNo))
            {
                // Extract the numeric part of the last reference number
                string numericPart = lastDoc.RefrenceNo.Substring(2); // Skip "Ch"
                int.TryParse(numericPart, out lastNumber);
            }

            // Increment the number and format it
            int newNumber = lastNumber + 1;
            string newReferenceNo = $"Ch{newNumber:D7}"; // Format as "Ch00001"

            return newReferenceNo;
        }

        public async Task<(string? Id, string? error)> GetRealName()
        {
            var claimsIdentity = _httpContext.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity == null)
            {
                return (null, "User identity is not available.");
            }
            // Try standard claim types first
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? claimsIdentity.FindFirst("sub")?.Value // OpenID Connect/JWT
                         ?? claimsIdentity.FindFirst("nameid")?.Value; // fallback

            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token.");
            }

            var RealName = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);


            return (RealName?.Realname, null);
        }

        public async Task<(string? Id, string? error)> GetUserId()
        {
            var claimsIdentity = _httpContext.HttpContext?.User.Identity as ClaimsIdentity;
            if (claimsIdentity == null)
            {
                return (null, "User identity is not available.");
            }
            // Try standard claim types first
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? claimsIdentity.FindFirst("sub")?.Value // OpenID Connect/JWT
                         ?? claimsIdentity.FindFirst("nameid")?.Value; // fallback

            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token.");
            }

            return (userId, null);
        }

        public async Task<string?> GetUserIpAddress()
        {
            var ip = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();
            return await Task.FromResult(ip);
        }
    }
}
