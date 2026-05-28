using SpaceXLaunches.Application.DTOs;
using SpaceXLaunches.Domain.Entities;
using SpaceXLaunches.Domain.Interfaces;

namespace SpaceXLaunches.Application.Services
{
    public class LaunchService
    {
        private readonly ILaunchRepository _launchRepository;
        private readonly ISpaceXService _spaceXService;

        public LaunchService(ILaunchRepository launchRepository, ISpaceXService spaceXService)
        {
            _launchRepository = launchRepository;
            _spaceXService = spaceXService;
        }

        public async Task<IEnumerable<LaunchDto>> GetAllLaunchesAsync()
        {
            bool hasData = await _launchRepository.AnyExistsAsync();

            if (hasData)
            {
                IEnumerable<Launch> cached = await _launchRepository.GetAllAsync();
                return cached.Select(MapToDto);
            }

            IEnumerable<Launch> launches = await _spaceXService.GetAllLaunchesAsync();
            await _launchRepository.SaveAllAsync(launches);
            return launches.Select(MapToDto);
        }

        public async Task<LaunchDto?> GetLaunchByIdAsync(string id)
        {
            bool exists = await _launchRepository.ExistsAsync(id);

            if (exists)
            {
                Launch? cached = await _launchRepository.GetByIdAsync(id);
                return cached is null ? null : MapToDto(cached);
            }

            Launch? launch = await _spaceXService.GetLaunchByIdAsync(id);

            if (launch is null)
            {
                return null;
            }

            await _launchRepository.SaveOneAsync(launch);
            return MapToDto(launch);
        }

        private static LaunchDto MapToDto(Launch launch)
        {
            return new LaunchDto(
                launch.Id,
                launch.FlightNumber,
                launch.Name,
                launch.DateUtc,
                launch.Success,
                launch.Details,
                launch.RocketId,
                launch.PatchSmall,
                launch.PatchLarge,
                launch.Webcast,
                launch.Article,
                launch.Wikipedia,
                launch.Failures
                    .Select(f => new LaunchFailureDto(f.TimeSeconds, f.AltitudeKm, f.Reason))
                    .ToList()
                    .AsReadOnly()
            );
        }
    }
}
