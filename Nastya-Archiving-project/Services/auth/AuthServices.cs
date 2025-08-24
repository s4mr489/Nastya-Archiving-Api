using AutoMapper;
using iText.StyledXmlParser.Css.Resolve.Shorthand.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Extinstion;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using Nastya_Archiving_project.Models.DTOs.Auth;
using Nastya_Archiving_project.Models.DTOs.Search.UsersSearch;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.SystemInfo;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using System.Net.WebSockets;

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

            // Defensive: Check for nulls in required int fields
            if (user.Id == 0 || user.Adminst == null)
                return "500";

            var JwtToken = new JwtToken(_context, _encryptionServices);
            //handle the user permissions and generate the JWT token
            var adminstDecrypted = _encryptionServices.DecryptString256Bit(user.Adminst);
            if (string.IsNullOrEmpty(adminstDecrypted))
                return "500";

            // Generate appropriate token based on user role
            string token;
            if (adminstDecrypted == "1" && IsAdmin)
            {
                token = JwtToken.GenToken(user.Id, "Admin", _configuration["Jwt:Issure"], 1, _configuration["Jwt:Key"]);
            }
            else if ((adminstDecrypted == "1" && !IsAdmin) || (adminstDecrypted == "0" && !IsAdmin))
            {
                token = JwtToken.GenToken(user.Id, "User", _configuration["Jwt:Issure"], 1, _configuration["Jwt:Key"]);
            }
            else if (adminstDecrypted == "0" && IsAdmin)
            {
                return "403";
            }
            else
            {
                return "500";
            }

            // Log the successful login
            await LogUserLogin(user);

            return token;
        }

        public async Task<(RegisterResponseDTOs? user, string? error)> Register(RegisterViewForm form, bool IsAdmin)
        {
            //check the user if exists or not
            var hashUserName = _encryptionServices.EncryptString256Bit(form.UserName);
            var user = await _context.Users.FirstOrDefaultAsync(e => e.UserName == hashUserName);
            if (user != null)
                return (null, "User already exists.");


            //handling the user properties  
            if ((await _infrastructureServices.GetAccountUintById(form.AccountUnitId)).accountUnits == null)
                return (null, "Account unit not found.");

            if ((await _infrastructureServices.GetBranchById(form.BranchId)).Branch == null)
                return (null, "Branch not found.");

            if ((await _infrastructureServices.GetDepartmentById(form.DeparId)).Department == null)
                return (null, "Depart not found.");
            if ((await _infrastructureServices.GetGrouptById(form.GroupId)).group == null)
                return (null, "Group not found.");
            if ((await _infrastructureServices.GetJobTitleById(form.JobTitle)).Job == null)
                return (null, "Job title not found.");

            //Insert The User To DataBase
            user = new User
            {
                AccountUnitId = form.AccountUnitId,
                BranchId = form.BranchId,
                DepariId = form.DeparId,
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

            // Add the user first
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Get group permissions to copy to the user
            var groupPermissions = await _context.Usersgroups
                .Where(g => g.groupid == user.GroupId)
                .FirstOrDefaultAsync();

            // Create new user permissions
            var userPermission = new UsersOptionPermission
            {
                UserId = user.Id,
                AllowDelete = groupPermissions?.AllowDelete ?? 0,
                AllowSendMail = groupPermissions?.AllowSendMail ?? 0,
                AllowDownload = groupPermissions?.AllowDownload ?? 0,
                AllowViewTheOther = groupPermissions?.AllowViewTheOther ?? 0,
                AllowAddToOther = groupPermissions?.AllowAddToOther ?? 0,
                AddParameters = groupPermissions?.AddParameters ?? 0
            };

            // Add the user permissions
            _context.UsersOptionPermissions.Add(userPermission);
            await _context.SaveChangesAsync();

            var result = new RegisterResponseDTOs
            {
                UserName = user.UserName,
                AccountUnitId = user.AccountUnitId,
                BranchId = user.BranchId,
                DeparId = user.DepariId,
                JobTitle = user.JobTitle,
                Realname = _encryptionServices.DecryptString256Bit(user.Realname),
                Id = user.Id,
                GroupId = user.GroupId,
                Permtype = _encryptionServices.DecryptString256Bit(user.Permtype),
                Adminst = _encryptionServices.DecryptString256Bit(user.Adminst),
                Editor = user.Editor,
            };

            return (result, null);

        }

        public async Task<string> ChangeUserPassword(ChangePasswordViewFrom pass)
        {
            var userId = 3;// _systemInfoServices.GetUserId().Id;
            var passowrd = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (passowrd == null)
                return "404";
            var hashCurrentPassword = _encryptionServices.EncryptString256Bit(pass.newPassword);
            if (hashCurrentPassword != passowrd.UserPassword)
                return "400";

            passowrd.UserPassword = hashCurrentPassword;

            _context.Users.Update(passowrd);
            await _context.SaveChangesAsync();

            return "200";
        }

        public async Task<(PagedList<UsersResponseDTOs>? users, string? error)> GetAllUsers(int pageNumber = 1, int pageSize = 10)
        {
            var usersQuery = _context.Users.AsQueryable();

            var totalCount = await usersQuery.CountAsync();
            if (totalCount == 0)
                return (null, "No users found.");

            var users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var usersDtoList = users
                .Where(u => u != null)
                .Select(u => new UsersResponseDTOs()
                {
                    Id = u.Id,
                    userName = _encryptionServices.DecryptString256Bit(u.UserName),
                    realName = _encryptionServices.DecryptString256Bit(u.Realname),
                    branch = _context.GpBranches.FirstOrDefault(b => b.Id == u.BranchId)?.Dscrp,
                    depart = _context.GpAccountingUnits.FirstOrDefault(a => a.Id == u.DepariId)?.Dscrp,
                    accountUnit = _context.GpAccountingUnits.FirstOrDefault(a => a.Id == u.AccountUnitId)?.Dscrp,
                    jobTitl = _context.PJobTitles.FirstOrDefault(j => j.Id == u.JobTitle)?.Dscrp,
                    permission = _encryptionServices.DecryptString256Bit(u.Adminst)
                })
                .Where(dto => dto != null)
                .ToList();

            if (usersDtoList == null || usersDtoList.Count == 0)
                return (null, "No users found.");

            var pagedList = new PagedList<UsersResponseDTOs>(usersDtoList, pageNumber, pageSize, totalCount);

            return (pagedList, null);
        }

        public async Task<string> RemoveUser(int Id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
            if (user == null)
                return "400";

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return "200";
        }

        public async Task<(RegisterResponseDTOs? user, string? error)> EditUser(int id, RegisterViewForm form, bool IsAdmin)
        {
            // Find the user by id
            var user = await _context.Users.FirstOrDefaultAsync(e => e.Id == id);
            if (user == null)
                return (null, "User not found.");

            // Validate related entities
            if ((await _infrastructureServices.GetAccountUintById(form.AccountUnitId)).accountUnits == null)
                return (null, "Account unit not found.");

            if ((await _infrastructureServices.GetBranchById(form.BranchId)).Branch == null)
                return (null, "Branch not found.");

            if ((await _infrastructureServices.GetDepartmentById(form.DeparId)).Department == null)
                return (null, "Depart not found.");

            if ((await _infrastructureServices.GetGrouptById(form.GroupId)).group == null)
                return (null, "Group not found.");

            if ((await _infrastructureServices.GetJobTitleById(form.JobTitle)).Job == null)
                return (null, "Job title not found.");

            // Update user properties
            user.AccountUnitId = form.AccountUnitId;
            user.BranchId = form.BranchId;
            user.DepariId = form.DeparId;
            user.JobTitle = form.JobTitle;
            user.Realname = _encryptionServices.EncryptString256Bit(form.Realname);
            user.UserName = _encryptionServices.EncryptString256Bit(form.UserName);
            // user.UserPassword = _encryptionServices.EncryptString256Bit(form.UserPassword);
            user.GroupId = form.GroupId;
            user.Permtype = _encryptionServices.EncryptString256Bit(form.Permtype);
            user.Adminst = _encryptionServices.EncryptString256Bit(IsAdmin ? "1" : "0");
            user.EditDate = DateOnly.FromDateTime(DateTime.Now);
            user.Editor = (await _systemInfoServices.GetRealName()).RealName;
            //user.AsmailCenter = form.AsmailCenter;
            //user.AsWfuser = form.AsWfuser;
            //user.DevisionId = form.DevisionId;
            //user.GobStep = form.GobStep;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<RegisterResponseDTOs>(user);

            return (result, null);
        }

        public async Task<(UsersResponseDTOs? user, string? error)> SearchUsers(string? realName, string? userName)
        {
            // Start with the Users DbSet as IQueryable
            var query = _context.Users.AsQueryable();

            // Apply filters using IQueryableExtensions.WhereFilter if you have a BaseFilter
            // Otherwise, filter manually for realName and userName
            if (!string.IsNullOrEmpty(realName))
            {
                query = query.Where(u => u.Realname.Contains(_encryptionServices.EncryptString256Bit(realName)));
            }
            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(u => u.UserName.Contains(_encryptionServices.EncryptString256Bit(userName)));
            }

            // Get the first matching user
            var user = await query.FirstOrDefaultAsync();
            if (user == null)
                return (null, "No user found.");


            var branch = await _context.GpBranches.FirstOrDefaultAsync(b => b.Id == user.BranchId);
            var depart = await _context.GpAccountingUnits.FirstOrDefaultAsync(a => a.Id == user.DepariId);
            var group = await _context.Usersgroups.FirstOrDefaultAsync(g => g.groupid == user.GroupId);
            var jobTitle = await _context.PJobTitles.FirstOrDefaultAsync(j => j.Id == user.JobTitle);
            var accountUnit = await _context.GpAccountingUnits.FirstOrDefaultAsync(a => a.Id == user.AccountUnitId);

            var userDto = new UsersResponseDTOs()
            {
                userName = _encryptionServices.DecryptString256Bit(user.UserName),
                realName = _encryptionServices.DecryptString256Bit(user.Realname),
                branch = branch?.Dscrp,
                depart = depart?.Dscrp,
                accountUnit = accountUnit?.Dscrp,
                jobTitl = jobTitle?.Dscrp,
                Id = user.Id,
                permission = _encryptionServices.DecryptString256Bit(user.Adminst),
            };
            return (userDto, null);
        }

        public async Task<BaseResponseDTOs> GetDepartForUsers(int userId)
        {
            // Get all archiving point permissions for the user
            var archivingPoints = await _context.UsersArchivingPointsPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();

            if (archivingPoints == null || archivingPoints.Count == 0)
                return new BaseResponseDTOs(null, 404, "No archiving points found for the user.");

            var archivingPointIds = archivingPoints
                .Select(p => p.ArchivingpointId ?? 0)
                .ToList();

            var archivingPointNames = await _context.PArcivingPoints
                .Where(a => archivingPointIds.Contains(a.Id))
                .ToListAsync();

            var result = archivingPoints
                .Select(p => new ArchivingPermissionResponseDTOs
                {
                    archivingPointId = p.ArchivingpointId ?? 0,
                    archivingPointDscrp = archivingPointNames
                        .Where(a => a.Id == p.ArchivingpointId)
                        .Select(a => a.Dscrp)
                        .FirstOrDefault()
                })
                .ToList();

            return new BaseResponseDTOs(result, 200);
        }
        private async Task LogUserLogin(User user)
        {
            try
            {
                // Create the log entry with login information
                var logEntry = new UsersEditing
                {
                    Model = "Auth",
                    TblName = "users",
                    TblNameA = "جدول المستخدمين",
                    RecordId = user.Id.ToString(),
                    RecordData = $"UserName={_encryptionServices.DecryptString256Bit(user.UserName)}#LoginTime={DateTime.UtcNow}",
                    OperationType = "Login",
                    AccountUnitId = user.AccountUnitId,
                    Editor = _encryptionServices.DecryptString256Bit(user.Realname),
                    EditDate = DateTime.UtcNow,
                    Ipadress = await _systemInfoServices.GetUserIpAddress()
                };

                // Add the entity to the DbSet
                _context.UsersEditings.Add(logEntry);

                // Save changes with current context
                await _context.SaveChangesAsync();

                // Optional: Log success to console for debugging
                Console.WriteLine($"Login logged: User ID {user.Id} at {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                // Log the exception details for troubleshooting
                Console.WriteLine($"Error logging user login: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}
