using Poprawa1.Models;
namespace Poprawa1.Services;


public interface IProjectService
{
    Task<ProjectDTO> GetProjectAsync(int id);
    Task AddArtifactAndProjectAsync(InputDTO inputDto);
}