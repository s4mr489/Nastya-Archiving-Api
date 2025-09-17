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
using Nastya_Archiving_project.Services.home;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.Limitation;
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
        private readonly ILimitationServices _limitationServices;

        public AuthServices(AppDbContext context,
                            IMapper mapper,
                            IConfiguration configuration,
                            IEncryptionServices encryptionServices,
                            IInfrastructureServices infrastructureServices,
                            IArchivingSettingsServicers archivingSettingsServicers,
                            ISystemInfoServices systemInfoServices,
                            ILimitationServices limitationServices) : base(mapper, context)
        {
            _mapper = mapper;
            _context = context;
            _configuration = configuration;
            _encryptionServices = encryptionServices;
            _infrastructureServices = infrastructureServices;
            _archivingSettingsServicers = archivingSettingsServicers;
            _systemInfoServices = systemInfoServices;
            _limitationServices = limitationServices;
        }

        public async Task<string> Login(LoginFormDTO form, bool IsAdmin)
        {
            // Check license validity first
            var limitResponse = await _limitationServices.ReadEncryptedTextFile();

            // Check if the license file exists and is valid
            if (limitResponse.StatusCode == 404)
            {
                // License file not found
                return "License not found. Please contact your system administrator.";
            }

            if (limitResponse.StatusCode == 200 && limitResponse.Data != null)
            {
                // Cast the dynamic Data property to access the License object
                dynamic responseData = limitResponse.Data;

                // Check if license has expired or is invalid
                if (responseData.License != null)
                {
                    // Check if license is expired
                    if (responseData.License.LicenseValidation.IsExpired)
                    {
                        return "Your license has expired. Please contact your system administrator.";
                    }

                }
            }

            else if (limitResponse.StatusCode != 200)
            {
                // Any other error with reading or processing the license
                return $"License error: {limitResponse.Error ?? "Unknown error"}";
            }

            //check the username and the password 
            var hashUserNamer = _encryptionServices.EncryptString256Bit(form.userName);
            var hashPassword = _encryptionServices.EncryptString256Bit(form.password);

            //search for the userName in the database
            var user = await _context.Users.FirstOrDefaultAsync(e => e.UserName == hashUserNamer);
            if (user == null)
                return "404";

            if (user.UserPassword != hashPassword)
                return "400";

            if (user.Stoped == 1)
                return "403";

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
            // Check license validity first
            var limitResponse = await _limitationServices.ReadEncryptedTextFile();

            // Check if the license file exists and is valid
            if (limitResponse.StatusCode == 404)
            {
                // License file not found
                return (null, "License not found. Please contact your system administrator.");
            }

            if (limitResponse.StatusCode == 200 && limitResponse.Data != null)
            {
                // Cast the dynamic Data property to access the License object
                dynamic responseData = limitResponse.Data;

                // Check if license has expired or is invalid
                if (responseData.License != null)
                {


                    // Check user count limitation if needed
                    int maxUsers = responseData.License.SystemLimits.MaxUsers;
                    int currentUsers = await _context.Users.Where(u => u.Stoped == 0).CountAsync();

                    // If we're registering a new user and reached the limit
                    if (currentUsers >= maxUsers)
                    {
                        return (null, "415");
                    }
                }
            }
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
                Address = form.Address,
                Email = form.Email,
                PhoneNo = form.PhoneNo,
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
                Email = user.Email,
                PhoneNo = user.PhoneNo,
                Address = user.Address
            };

            return (result, null);

        }

        public async Task<string> ChangeUserPassword(ChangePasswordViewFrom pass)
        {
            // Get user ID as a tuple with possible error
            var userIdResult = await _systemInfoServices.GetUserId();

            // Check if there was an error getting the user ID
            if (userIdResult.error != null)
                return "401"; // Unauthorized - user not authenticated properly

            // Parse the user ID to integer
            if (!int.TryParse(userIdResult.Id, out int userId))
                return "400"; // Bad request - invalid user ID format

            // Find the user in the database
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
                return "404"; // Not found - user doesn't exist

            // Validate current password
            var hashCurrentPassword = _encryptionServices.EncryptString256Bit(pass.CurrnetPassword);
            if (hashCurrentPassword != user.UserPassword)
                return "400"; // Bad request - incorrect current password


            // Update password
            user.UserPassword = _encryptionServices.EncryptString256Bit(pass.newPassword);

            // Save changes
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return "200"; // Success
        }

        public async Task<(PagedList<UsersResponseDTOs>? users, string? error)> GetAllUsers(string realName, int pageNumber = 1, int pageSize = 10)
        {
            var usersQuery = _context.Users.AsQueryable();
            List<User> users;

            if (!string.IsNullOrEmpty(realName))
            {
                // Fetch all users for in-memory filtering since real names are encrypted
                users = await usersQuery.ToListAsync();

                // Filter by decrypted real name: exact or partial match (case-insensitive)
                users = users
                    .Where(u =>
                    {
                        var decryptedName = _encryptionServices.DecryptString256Bit(u.Realname ?? "");
                        return !string.IsNullOrEmpty(decryptedName) &&
                               (decryptedName.Equals(realName, StringComparison.OrdinalIgnoreCase) ||
                                decryptedName.Contains(realName, StringComparison.OrdinalIgnoreCase));
                    })
                    .ToList();
            }
            else
            {
                users = await usersQuery.ToListAsync();
            }

            var totalCount = users.Count;
            if (totalCount == 0)
                return (null, "No users found.");

            // Apply paging after filtering
            var pagedUsers = users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var usersDtoList = pagedUsers
                .Where(u => u != null)
                .Select(u => new UsersResponseDTOs()
                {
                    Id = u.Id,
                    userName = _encryptionServices.DecryptString256Bit(u.UserName),
                    realName = _encryptionServices.DecryptString256Bit(u.Realname),
                    branch = _context.GpBranches.FirstOrDefault(b => b.Id == u.BranchId)?.Dscrp,
                    depart = _context.GpAccountingUnits.FirstOrDefault(a => a.Id == u.DepariId)?.Dscrp,
                    group = _context.Usersgroups.FirstOrDefault(g => g.groupid == u.GroupId)?.Groupdscrp,
                    accountUnit = _context.GpAccountingUnits.FirstOrDefault(a => a.Id == u.AccountUnitId)?.Dscrp,
                    jobTitl = _context.PJobTitles.FirstOrDefault(j => j.Id == u.JobTitle)?.Dscrp,
                    permission = _encryptionServices.DecryptString256Bit(u.Adminst),
                    address = u.Address,
                    email = u.Email,
                    phoneNo = u.PhoneNo
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
            user.UserPassword = _encryptionServices.EncryptString256Bit(form.UserPassword);
            user.GroupId = form.GroupId;
            user.Permtype = _encryptionServices.EncryptString256Bit(form.Permtype);
            user.Adminst = _encryptionServices.EncryptString256Bit(IsAdmin ? "1" : "0");
            user.EditDate = DateOnly.FromDateTime(DateTime.Now);
            user.Editor = (await _systemInfoServices.GetRealName()).RealName;
            user.Stoped = user.Stoped; // Retain existing status
            user.Address = form.Address ?? user.Address;
            user.Email = form.Email ?? user.Email;
            user.PhoneNo = form.PhoneNo ?? user.PhoneNo;    
            //user.AsmailCenter = form.AsmailCenter;
            //user.AsWfuser = form.AsWfuser;
            //user.DevisionId = form.DevisionId;
            //user.GobStep = form.GobStep;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var result = new RegisterResponseDTOs
            {
                AccountUnitId = user.AccountUnitId,
                BranchId = user.BranchId,
                DeparId = user.DepariId,
                JobTitle = form.JobTitle,
                Realname = _encryptionServices.DecryptString256Bit(user.Realname),
                UserName = _encryptionServices.DecryptString256Bit(user.UserName),
                Id = user.Id,
                GroupId = user.GroupId,
                Permtype = _encryptionServices.DecryptString256Bit(user.Permtype),
                Adminst = _encryptionServices.DecryptString256Bit(user.Adminst),
                Editor = user.Editor,
                Address = user.Address , 
                PhoneNo = user.PhoneNo, 
                Email = user.Email 
            };

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
            var depart = await _context.GpDepartments.FirstOrDefaultAsync(a => a.Id == user.DepariId);
            var group = await _context.Usersgroups.FirstOrDefaultAsync(g => g.groupid == user.GroupId);
            var jobTitle = await _context.PJobTitles.FirstOrDefaultAsync(j => j.Id == user.JobTitle);
            var accountUnit = await _context.GpAccountingUnits.FirstOrDefaultAsync(a => a.Id == user.AccountUnitId);

            var userDto = new UsersResponseDTOs()
            {
                userName = _encryptionServices.DecryptString256Bit(user.UserName),
                realName = _encryptionServices.DecryptString256Bit(user.Realname),
                branch = branch?.Dscrp,
                depart = depart?.Dscrp,
                group = _encryptionServices.DecryptString256Bit(group.Groupdscrp),
                accountUnit = accountUnit?.Dscrp,
                jobTitl = jobTitle?.Dscrp,
                Id = user.Id,
                permission = _encryptionServices.DecryptString256Bit(user.Adminst),
                address = user.Address ,
                phoneNo = user.PhoneNo ,
                email = user.Email 
            };
            return (userDto, null);
        }

        public async Task<BaseResponseDTOs> GetDepartForUsers(int userId)
        {
            // First get the user's primary department from the Users table
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return new BaseResponseDTOs(null, 404, "User not found.");

            var result = new List<ArchivingPermissionResponseDTOs>();

            // If user has a department, add it as the first item
            if (user.DepariId.HasValue && user.DepariId.Value > 0)
            {
                var primaryDepartment = await _context.GpDepartments
                    .FirstOrDefaultAsync(d => d.Id == user.DepariId.Value);

                if (primaryDepartment != null)
                {
                    result.Add(new ArchivingPermissionResponseDTOs
                    {
                        departId = primaryDepartment.Id,
                        departmentName = primaryDepartment.Dscrp,
                        archivingPointId = 0, // Default value since this is primary department
                        archivingPointDscrp = "Primary Department" // Indicate this is the primary department
                    });
                }
            }

            // Get all archiving point permissions for the user
            var archivingPoints = await _context.UsersArchivingPointsPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();

            if (archivingPoints.Count == 0 && result.Count == 0)
                return new BaseResponseDTOs(null, 404, "No departments or archiving points found for the user.");

            var archivingPointIds = archivingPoints
                .Where(p => p.ArchivingpointId.HasValue && p.ArchivingpointId.Value > 0)
                .Select(p => p.ArchivingpointId.Value)
                .ToList();

            var archivingPointNames = await _context.PArcivingPoints
                .Where(a => archivingPointIds.Contains(a.Id))
                .ToListAsync();

            // Collect department IDs from both UsersArchivingPointsPermission and PArcivingPoints
            var departmentIds = new HashSet<int>();

            // First add department IDs directly from permissions
            foreach (var permission in archivingPoints)
            {
                if (permission.DepartId.HasValue && permission.DepartId.Value > 0)
                {
                    departmentIds.Add(permission.DepartId.Value);
                }
            }

            // Then also get department IDs from archiving points as a fallback
            foreach (var point in archivingPointNames)
            {
                if (point.DepartId.HasValue && point.DepartId.Value > 0)
                {
                    departmentIds.Add(point.DepartId.Value);
                }
            }

            // Get department information
            var departments = await _context.GpDepartments
                .Where(d => departmentIds.Contains(d.Id))
                .ToListAsync();

            // Add additional departments from archiving points to the result
            var additionalDepartments = archivingPoints.Select(p =>
            {
                // Try to get department ID first from permission, then from archiving point
                int? departmentId = p.DepartId;
                if ((!departmentId.HasValue || departmentId.Value == 0) && p.ArchivingpointId.HasValue)
                {
                    var archivingPoint = archivingPointNames.FirstOrDefault(a => a.Id == p.ArchivingpointId.Value);
                    if (archivingPoint != null)
                    {
                        departmentId = archivingPoint.DepartId;
                    }
                }

                // Get department name based on the department ID
                string departmentName = null;
                if (departmentId.HasValue && departmentId.Value > 0)
                {
                    var department = departments.FirstOrDefault(d => d.Id == departmentId.Value);
                    if (department != null)
                    {
                        departmentName = department.Dscrp;
                    }
                }

                // Skip if it's the same as the user's primary department
                if (departmentId.HasValue && user.DepariId.HasValue && departmentId.Value == user.DepariId.Value)
                    return null;

                return new ArchivingPermissionResponseDTOs
                {
                    archivingPointId = p.ArchivingpointId ?? 0,
                    archivingPointDscrp = archivingPointNames
                        .FirstOrDefault(a => a.Id == p.ArchivingpointId)?.Dscrp,
                    departId = departmentId ?? 0,
                    departmentName = departmentName
                };
            })
            .Where(dto => dto != null)
            .ToList();

            // Add the additional departments to the result
            result.AddRange(additionalDepartments);

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

        public async Task<BaseResponseDTOs> FirstUsers(LoginFormDTO req)
        {
            var userCount = await _context.Users.CountAsync();
            var seeder = new Seeder(_context, _encryptionServices);
            if (userCount == 0)
            {
                // Fix: add await to properly wait for the SeedSuperAdmin method to complete
                await seeder.SeedSuperAdmin(req.userName, req.password);
                return new BaseResponseDTOs(null, 200, "The First User In The System Created Successfully.");
            }
            return new BaseResponseDTOs(null, 400, "There Are Users In The DB.");
        }

        public async Task<BaseResponseDTOs> ActiveOrDeActivUser(int Id, bool status)
        {
            try
            {
                // Find the user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Id);
                if (user == null)
                    return new BaseResponseDTOs(null, 404, "User not found.");

                // Get current status for change detection
                int previousStatus = user.Stoped ?? 1; // Default to inactive if null

                // Get license information - we need this for both activation and deactivation
                var limitResponse = await _limitationServices.ReadEncryptedTextFile();

                // Check if the license file exists and is valid
                if (limitResponse.StatusCode == 404)
                {
                    return new BaseResponseDTOs(null, 403, "License not found. Cannot change user status.");
                }

                if (limitResponse.StatusCode == 200 && limitResponse.Data != null)
                {
                    // Cast the dynamic Data property to access the License object
                    dynamic licenseData = limitResponse.Data;

                    // Check if license has expired or is invalid
                    if (licenseData.License != null)
                    {
                        // Check if license is expired
                        if (licenseData.License.LicenseValidation.IsExpired)
                        {
                            return new BaseResponseDTOs(null, 403, "Your license has expired. Cannot change user status.");
                        }

                        int maxUsers = licenseData.License.SystemLimits.MaxUsers;

                        // Count all active users excluding the current user
                        int currentActiveUsers = await _context.Users
                            .Where(u => u.Stoped == 0 && u.Id != Id)
                            .CountAsync();

                        // Count all users (both active and inactive)
                        int totalUsers = await _context.Users.CountAsync();

                        // Check based on the operation we're performing
                        if (status && previousStatus == 1) // Activating a user
                        {
                            // Check if activating this user would exceed the max active users limit
                            if (currentActiveUsers + 1 > maxUsers)
                            {
                                return new BaseResponseDTOs(null, 415,
                                    $"Maximum active user limit ({maxUsers}) reached. Please upgrade your license or deactivate other users before activating this one.");
                            }
                        }
                        else if (!status && previousStatus == 0) // Deactivating a user
                        {
                            // Check if we have sufficient active users allowed by license
                            // This is a safeguard to ensure minimum operational requirements
                            if (currentActiveUsers <= 1 && totalUsers > 1)
                            {
                                return new BaseResponseDTOs(null, 415,
                                    "Cannot deactivate this user as it would leave the system without any active users.");
                            }

                            // Note: We allow deactivation even if total user count exceeds license
                            // since deactivation helps comply with the active user count limit
                        }
                    }
                }
                else if (limitResponse.StatusCode != 200)
                {
                    // Any other error with reading or processing the license
                    return new BaseResponseDTOs(null, 500,
                        $"License error: {limitResponse.Error ?? "Unknown error"}. Cannot change user status.");
                }

                // Update user status (0 = active, 1 = inactive)
                user.Stoped = status ? 0 : 1;

                // Update the user without creating log entry
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Get decrypted user information for the response
                var userName = _encryptionServices.DecryptString256Bit(user.UserName ?? string.Empty);
                var realName = _encryptionServices.DecryptString256Bit(user.Realname ?? string.Empty);

                // Prepare the response
                var message = status
                    ? "User activated successfully."
                    : "User deactivated successfully.";

                // Add license info to response
                var userData = new
                {
                    userId = user.Id,
                    userName = userName,
                    realName = realName,
                    newStatus = user.Stoped,
                    isActive = status
                };

                // Get updated user counts for the response message
                int activeUsers = await _context.Users.Where(u => u.Stoped == 0).CountAsync();
                int inactiveUsers = await _context.Users.Where(u => u.Stoped == 1 || u.Stoped == null).CountAsync();

                // Get max users from license for reference
                int maxAllowedUsers = 0;
                if (limitResponse.StatusCode == 200 && limitResponse.Data != null)
                {
                    dynamic licData = limitResponse.Data;
                    if (licData.License != null && licData.License.SystemLimits != null)
                    {
                        maxAllowedUsers = licData.License.SystemLimits.MaxUsers;
                    }
                }

                // Include license info in the message for both activation and deactivation
                message += $" Active users: {activeUsers}/{maxAllowedUsers}, Inactive users: {inactiveUsers}.";

                return new BaseResponseDTOs(userData, 200, message);
            }
            catch (Exception ex)
            {
                // Log the exception details for troubleshooting (console only, not database)
                Console.WriteLine($"Error updating user status: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return new BaseResponseDTOs(null, 500,
                    $"An error occurred while {(status ? "activating" : "deactivating")} the user: {ex.Message}");
            }
        }
    }
}
