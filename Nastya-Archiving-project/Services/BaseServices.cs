using AutoMapper;
using Nastya_Archiving_project.Data;

namespace Nastya_Archiving_project.Services
{
    public class BaseServices
    {
        protected readonly AppDbContext _context;
        protected readonly IMapper _mapper;

        public BaseServices(IMapper mapper, AppDbContext context)
        {
            _mapper = mapper;
            _context = context;
        }
    }
}
