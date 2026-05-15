using DocumentService.Data;
using DocumentService.DTOs.Responses;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly DocumentDbContext _context;

        public SubjectService(DocumentDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubjectResponse>> GetAllSubjectsAsync()
        {
            return await _context.Subjects
                .Select(s => new SubjectResponse
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Description = s.Description
                })
                .ToListAsync();
        }
    }
}
