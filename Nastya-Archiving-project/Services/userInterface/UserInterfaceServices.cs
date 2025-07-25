﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs.UserInterface;
using Nastya_Archiving_project.Services.SystemInfo;

namespace Nastya_Archiving_project.Services.userInterface
{
    public class UserInterfaceServices : BaseServices , IUserInterfaceServices
    {
        private readonly ISystemInfoServices _systemInfoServices;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;


        public UserInterfaceServices(AppDbContext context , IMapper mapper, ISystemInfoServices systemInfo) :base(mapper, context)
        {
            _context = context;
            _mapper = mapper;
            _systemInfoServices = systemInfo;
        }

        public async Task<string> CreateUserInterface(UserInterfaceViewForm requestDTOs)
        {

            var page = await _context.Usersinterfaces
                .FirstOrDefaultAsync(ui => ui.Pageurl == requestDTOs.pageUrl);

            if (page != null)
                return "400"; // this page already exists

            page = _mapper.Map<Usersinterface>(requestDTOs);
            _context.Usersinterfaces.Add(page);
            await _context.SaveChangesAsync();
            return "200"; // user interface created successfully

        }

        public async Task<Dictionary<string, List<UserInterfaceResponseDTOs>>> GetPageUrlsGroupedByOutputType()
        {
            var result = await _context.Usersinterfaces
               .Where(ui => ui.Outputtype != null && ui.Pageurl != null)
               .OrderBy(ui => ui.Outputtype)
               .GroupBy(ui => ui.Outputtype)
               .ToDictionaryAsync(
                   g => g.Key!,
                   g => g.Select(ui => new UserInterfaceResponseDTOs
                   {
                       pageId = ui.Id,
                       pageUrl = ui.Pageurl,
                       pageDescription = ui.Pagedscrp
                   }).ToList()
               );

            return result;
        }

        public async Task<(List<UserInterfaceResponseDTOs>? urls, string? error)> GetUserInterfaceForUser()
        {
            var userResult = await _systemInfoServices.GetUserId();
            if (userResult.Id == null)
            {
                return (null, "401"); // Unauthorized access, user ID not found
            }

            if (!int.TryParse(userResult.Id, out int userId))
            {
                return (null, "400"); // Invalid user ID format
            }

            var groupId = await _context.Users
                 .Where(u => u.Id == userId)
                 .Select(u => u.GroupId)
                 .FirstOrDefaultAsync();

            if (groupId == null)
            {
                return (null, "404"); // group not found 
            }

            var pageIds = await _context.Userspermissions
                .Where(up => up.Groupid == groupId && up.Pageid != null)
                .Select(up => up.Pageid)
                .ToListAsync();

            var intPageIds = pageIds
                .Where(pid => int.TryParse(pid, out _))
                .Select(pid => int.Parse(pid))
                .ToList();

            if (intPageIds.Count == 0)
            {
                return (new List<UserInterfaceResponseDTOs>(), null);
            }

            var urls = await _context.Usersinterfaces
                .Where(ui => intPageIds.Contains(ui.Id))
                .Select(ui => new UserInterfaceResponseDTOs
                {
                    pageId = ui.Id,
                    pageUrl = ui.Pageurl,
                    pageDescription = ui.Pagedscrp
                })
                .ToListAsync();

            return (urls, null);
        }
    }
}
