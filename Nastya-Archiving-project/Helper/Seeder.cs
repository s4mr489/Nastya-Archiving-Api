using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Services.encrpytion;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using System.Reflection.Metadata.Ecma335;

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
            var superAdmin = await _context.Users.FirstOrDefaultAsync(u => u.UserName == _encryptionServices.EncryptString256Bit(userName));
            if (superAdmin == null)
            {
                //create the super admin
                var newUser = new User
                {
                    UserName = _encryptionServices.EncryptString256Bit(userName),
                    UserPassword = _encryptionServices.EncryptString256Bit(password),
                    Adminst = _encryptionServices.EncryptString256Bit("1"),
                    Realname = _encryptionServices.EncryptString256Bit("System Administrator"),
                    Permtype = _encryptionServices.EncryptString256Bit("Admin"),
                    Editor = "System",
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                var permission = await _context.UsersOptionPermissions.FirstOrDefaultAsync(p => p.UserId == newUser.Id);
                if (permission == null)
                {
                    var newPermission = new UsersOptionPermission
                    {
                        UserId = newUser.Id,
                        AddParameters = 1,
                        AllowViewTheOther = 1,
                        AllowDownload = 1,
                        AllowAddToOther = 1,
                        AllowDelete = 1,
                        AllowSendMail = 1,
                    };
                    _context.UsersOptionPermissions.Add(newPermission);
                    await _context.SaveChangesAsync();
                }
            }
        }
      
    }
}
