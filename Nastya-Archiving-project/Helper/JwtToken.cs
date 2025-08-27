using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Services.encrpytion;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nastya_Archiving_project.Helper
{
    public class JwtToken
    {
        private readonly AppDbContext _context;
        private readonly IEncryptionServices _encryptionServices;

        public JwtToken(AppDbContext context, IEncryptionServices encryptionServices = null)
        {
            _context = context;
            _encryptionServices = encryptionServices;
        }

        public string GenToken(int userId, string role, string issuer, int dayes, string secertKey)
        {
            if (string.IsNullOrEmpty(secertKey))
                throw new ArgumentException("Secret key cannot be null or empty.", nameof(secertKey));

            // Get user information from database
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                throw new ArgumentException("User not found.", nameof(userId));

            // Load related entities synchronously 
            var branch = _context.GpBranches.FirstOrDefault(b => b.Id == user.BranchId);
            var depart = _context.GpDepartments.FirstOrDefault(d => d.Id == user.DepariId);
            var group = _context.Usersgroups.FirstOrDefault(g => g.groupid == user.GroupId);
            var jobTitle = _context.PJobTitles.FirstOrDefault(j => j.Id == user.JobTitle);
            var accountUnit = _context.GpAccountingUnits.FirstOrDefault(a => a.Id == user.AccountUnitId);

            byte[] Bytes = Encoding.ASCII.GetBytes(secertKey);
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("RealName", (_encryptionServices?.DecryptString256Bit(user.UserName) ?? user.UserName)),
                    new Claim("BranchId", user.BranchId?.ToString() ?? string.Empty),
                    new Claim("DepartId", user.DepariId?.ToString() ?? string.Empty),
                    new Claim("AccountUnitId", user.AccountUnitId?.ToString() ?? string.Empty),
                    new Claim("FileType", user.AsWfuser?.ToString() ?? string.Empty),
                    new Claim("SBranch", branch?.Dscrp ?? string.Empty),
                    new Claim("Sdepart", depart?.Dscrp ?? string.Empty),
                    new Claim("Sgroup", group != null && _encryptionServices != null
                        ? _encryptionServices.DecryptString256Bit(group.Groupdscrp) ?? string.Empty
                        : string.Empty),
                    new Claim("SaccountUnit", accountUnit?.Dscrp ?? string.Empty),
                }),
                Expires = DateTime.UtcNow.AddDays(dayes),
                Issuer = issuer,
                SigningCredentials = new SigningCredentials(
                                        new SymmetricSecurityKey(Bytes),
                                        SecurityAlgorithms.HmacSha256Signature)
            };
            SecurityToken token = jwtSecurityTokenHandler.CreateToken(tokenDescriptor);
            return jwtSecurityTokenHandler.WriteToken(token);
        }
    }
}