using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Services.encrpytion;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Nastya_Archiving_project.Helper
{
    public class Seeder
    {
        private readonly AppDbContext _context;
        private readonly IEncryptionServices _encryptionServices;
        public Seeder(AppDbContext context, IEncryptionServices encryptionServices)
        {
            _context = context;
            _encryptionServices = encryptionServices;
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
                    UserName = _encryptionServices.EncryptString256Bit(userName),
                    UserPassword = BCrypt.Net.BCrypt.HashPassword(password),
                    Adminst = _encryptionServices.EncryptString256Bit("1"),
                    Realname = _encryptionServices.EncryptString256Bit("System Administrator"),
                    Permtype = _encryptionServices.EncryptString256Bit("Admin"),
                    Editor = "System",
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
        }
    }
}
