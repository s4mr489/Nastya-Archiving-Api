using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Models.DTOs.Auth;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Nastya_Archiving_project.Services.auth
{
    public class AuthServices : BaseServices, IAuthServices
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public AuthServices(AppDbContext context, IMapper mapper, IConfiguration configuration) : base(mapper, context)
        {
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
        }


        public async Task<string> Login(LoginFormDTO form, bool IsAdmin)
        {
            //search for the userName in the database
            var user = await _context.Users.FirstOrDefaultAsync(e => e.UserName == form.userName);
            if (user == null)
                return "404";
            //check the user Passsowd
            if (!BCrypt.Net.BCrypt.Verify(form.password, user.UserPassword))
                return "400";

            //handle the user permissions and generate the JWT token
            if (user.Permtype == "Admin" && IsAdmin)
            {
                var token = JwtToken.GenToken(user.Id, user.Permtype, _configuration["Jwt:Issure"], 1, _configuration["Jwt:Key"]);
                return token;
            }
            if (user.Permtype == "Admin" && !IsAdmin)
            {
                var token = JwtToken.GenToken(user.Id, "User", _configuration["Jwt:Issure"], 1, _configuration["Jwt:Key"]);
                return token;
            }
            if (user.Permtype == "User" && !IsAdmin)
            {
                var token = JwtToken.GenToken(user.Id, user.Permtype, _configuration["Jwt:Issure"], 1, _configuration["Jwt:Key"]);
            }
            if (user.Permtype == "User" && IsAdmin)
            {
                return "403";
            }
            return "500"; // In case of any other unexpected error
        }


    }
}
