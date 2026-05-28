using SpaceXLaunches.Domain.Entities;

namespace SpaceXLaunches.Domain.Interfaces
{
    public interface ILaunchRepository
    {
        Task<IEnumerable<Launch>> GetAllAsync();
        Task<Launch?> GetByIdAsync(string id);
        Task SaveAllAsync(IEnumerable<Launch> launches);
        Task SaveOneAsync(Launch launch);
        Task<bool> ExistsAsync(string id);
        Task<bool> AnyExistsAsync();
    }
}
