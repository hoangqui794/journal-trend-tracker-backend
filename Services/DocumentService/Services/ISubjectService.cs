using DocumentService.DTOs.Responses;

namespace DocumentService.Services
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectResponse>> GetAllSubjectsAsync();
    }
}
