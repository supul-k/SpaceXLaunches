using SpaceXLaunches.Domain.Common;
using SpaceXLaunches.Domain.Entities;

namespace SpaceXLaunches.Domain.Interfaces
{
    public interface ISpaceXService
    {
        Task<Result<IEnumerable<Launch>>> GetAllLaunchesAsync();
        Task<Result<Launch>> GetLaunchByIdAsync(string id);
    }
}
