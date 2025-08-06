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

            // Example: Get user real name from database
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            var realName = user?.Realname ?? string.Empty;

            byte[] Bytes = Encoding.ASCII.GetBytes(secertKey);
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("RealName", (_encryptionServices.DecryptString256Bit(user.UserName))),
                    new Claim("BranchId", user?.BranchId.ToString()),
                    new Claim("DepartId", user?.DepariId.ToString()),
                    new Claim("AccountUnitId", user?.AccountUnitId.ToString()),
                    new Claim("FileType" , user?.AsWfuser.ToString()),
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