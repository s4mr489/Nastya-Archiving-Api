using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Nastya_Archiving_project.Helper
{
    public class Seeder
    {
        private readonly AppDbContext _context;
        public Seeder(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedSuperAdmin(string userName, string password)
        {
            //check if the super admin alreaydy exists
            var superAdmin = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (superAdmin == null)
            {
                //create the super admin
                var newUser = new User
                {
                    UserName = userName,
                    UserPassword = BCrypt.Net.BCrypt.HashPassword(password),
                    Adminst = "True",
                    Realname = "System Administrator",
                    Permtype = "Admin",
                    Editor = "System",
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
        }
    }
}
