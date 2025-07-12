using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Services.encrpytion;
using System.Security.Claims;

namespace Nastya_Archiving_project.Services.SystemInfo
{
    public class SystemInfoServices : BaseServices , ISystemInfoServices
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IEncryptionServices _encryptionServices;
        public SystemInfoServices(AppDbContext context, IHttpContextAccessor httpContext, IEncryptionServices encryptionServices) : base(null, context)
        {
            _context = context;
            _httpContext = httpContext;
            _encryptionServices = encryptionServices;
        }

        public async Task<string> GetLastRefNo()
        {
            var lastDoc = await _context.ArcivingDocs.CountAsync();
            var lastDelete = await _context.ArcivingDocsDeleteds.CountAsync();

            

            // Increment the number and format it
            int newNumber = lastDelete+lastDoc + 1;
            string newReferenceNo = $"Ch{newNumber:D8}"; // Format as "Ch00001"

            return newReferenceNo;
        }

        public async Task<(string? RealName, string? error)> GetRealName()
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


            return (_encryptionServices.DecryptString256Bit(RealName?.Realname), null);
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
