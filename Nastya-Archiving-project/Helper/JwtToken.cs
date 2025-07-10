using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nastya_Archiving_project.Helper
{
    public static class JwtToken
    {
        private static SymmetricSecurityKey _keyl;
        public static string GenToken(int userId , string role , string issuer , int dayes , string secertKey)
        {
            if(string.IsNullOrEmpty(secertKey))
                throw new ArgumentException("Secret key cannot be null or empty.", nameof(secertKey));

            byte[] Bytes = Encoding.ASCII.GetBytes(secertKey);
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {

                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
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
