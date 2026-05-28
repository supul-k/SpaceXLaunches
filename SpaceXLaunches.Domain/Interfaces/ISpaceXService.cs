using SpaceXLaunches.Domain.Entities;

namespace SpaceXLaunches.Domain.Interfaces
{
    public interface ISpaceXService
    {
        Task<IEnumerable<Launch>> GetAllLaunchesAsync();
        Task<Launch?> GetLaunchByIdAsync(string id);
    }
}
