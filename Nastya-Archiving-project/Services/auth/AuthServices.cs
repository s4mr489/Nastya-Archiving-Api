using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs.Auth;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Nastya_Archiving_project.Services.auth
{
    public class AuthServices : BaseServices, IAuthServices
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly IEncryptionServices _encryptionServices;
        private readonly IInfrastructureServices _infrastructureServices;
        private readonly IArchivingSettingsServicers _archivingSettingsServicers;
        private readonly ISystemInfoServices _systemInfoServices;

        public AuthServices(AppDbContext context,
                            IMapper mapper,
                            IConfiguration configuration,
                            IEncryptionServices encryptionServices,
                            IInfrastructureServices infrastructureServices,
                            IArchivingSettingsServicers archivingSettingsServicers,
                            ISystemInfoServices systemInfoServices) : base(mapper, context)
        {
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _encryptionServices = encryptionServices;
            _infrastructureServices = infrastructureServices;
            _archivingSettingsServicers = archivingSettingsServicers;
            _systemInfoServices = systemInfoServices;
        }


        public async Task<string> Login(LoginFormDTO form, bool IsAdmin)
        {
            //check the username and the password 
            var hashUserNamer = _encryptionServices.EncryptString256Bit(form.userName);
            var hashPassword = _encryptionServices.EncryptString256Bit(form.password);

            //search for the userName in the database
            var user = await _context.Users.FirstOrDefaultAsync(e => e.UserName == hashUserNamer);
            if (user == null)
                return "404";
            if (user.UserPassword != hashPassword)
                return "400";

            var JwtToken = new JwtToken(_context , _encryptionServices);
            //handle the user permissions and generate the JWT token
            if (_encryptionServices.DecryptString256Bit(user.Adminst) == "1" && IsAdmin)
            {
                var token = JwtToken.GenToken(user.Id, "Admin", _configuration["Jwt:Issure"], 1, _configuration["Jwt:Key"]);
                return token;
            }
            if (_encryptionServices.DecryptString256Bit(user.Adminst) == "1" && !IsAdmin)
            {
                var token = JwtToken.GenToken(user.Id, "User", _configuration["Jwt:Issure"], 1, _configuration["Jwt:Key"]);
                return token;
            }
            if (_encryptionServices.DecryptString256Bit(user.Adminst) == "0" && !IsAdmin)
            {
                var token = JwtToken.GenToken(user.Id, "User", _configuration["Jwt:Issure"], 1, _configuration["Jwt:Key"]);
                return token;
            }
            if (_encryptionServices.DecryptString256Bit(user.Adminst) == "0" && IsAdmin)
            {
                return "403";
            }
            return "500"; // In case of any other unexpected error
        }

        public async Task<(RegisterResponseDTOs? user, string? error)> Register(RegisterViewForm form, bool IsAdmin)
        {
            //check the user if exists or not
            var hashUserName = _encryptionServices.EncryptString256Bit(form.UserName);
            var user = await _context.Users.FirstOrDefaultAsync(e => e.UserName == hashUserName);
            if(user != null)
                return (null, "User already exists.");


            //handling the user properties  
            if ((await _infrastructureServices.GetAccountUintById(form.AccountUnitId)).accountUnits == null)
                return (null, "Account unit not found.");

            if((await _infrastructureServices.GetBranchById(form.BranchId)).Branch == null)
                return (null, "Branch not found.");

            if((await _infrastructureServices.GetDepartmentById(form.DepariId)).Department == null)
                return (null, "Depart not found.");
            if((await _infrastructureServices.GetGrouptById(form.GroupId)).group == null)
                return (null, "Group not found.");
            if ((await _infrastructureServices.GetJobTitleById(form.JobTitle)).Job == null)
                return (null, "Job title not found.");

            //Insert The User To DataBase
            user = new User
            {
                AccountUnitId = form.AccountUnitId,
                BranchId = form.BranchId,
                DepariId = form.DepariId,
                JobTitle = form.JobTitle,
                Realname = _encryptionServices.EncryptString256Bit(form.Realname),
                UserName = _encryptionServices.EncryptString256Bit(form.UserName),
                UserPassword = _encryptionServices.EncryptString256Bit(form.UserPassword),
                GroupId = form.GroupId,
                Permtype = _encryptionServices.EncryptString256Bit(form.Permtype),
                Adminst = _encryptionServices.EncryptString256Bit(IsAdmin ? "1" : "0"),
                EditDate = DateOnly.FromDateTime(DateTime.Now),
                Editor = (await _systemInfoServices.GetRealName()).RealName,
                //AsmailCenter = form.AsmailCenter,
                //AsWfuser = form.AsWfuser,
                //DevisionId = form.DevisionId,  // this prop null until understand it 
                //GobStep = form.GobStep,
                
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<RegisterResponseDTOs>(user);

            return (result, null);

        }
    }
}
