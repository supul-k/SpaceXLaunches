using SpaceXLaunches.Domain.Common;
using SpaceXLaunches.Domain.Entities;

namespace SpaceXLaunches.Domain.Interfaces
{
    public interface ILaunchRepository
    {
        Task<Result<IEnumerable<Launch>>> GetAllAsync();
        Task<Result<Launch>> GetByIdAsync(string id);
        Task<Result<bool>> ExistsAsync(string id);
        Task<Result<bool>> AnyExistsAsync();
        Task<Result<bool>> SaveAllAsync(IEnumerable<Launch> launches);
        Task<Result<bool>> SaveOneAsync(Launch launch);
    }
}
