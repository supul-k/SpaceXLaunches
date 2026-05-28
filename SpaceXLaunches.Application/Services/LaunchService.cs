using SpaceXLaunches.Application.DTOs;
using SpaceXLaunches.Domain.Common;
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

        public async Task<Result<IEnumerable<LaunchDto>>> GetAllLaunchesAsync()
        {
            Result<bool> anyExists = await _launchRepository.AnyExistsAsync();

            if (anyExists.IsFailure)
            {
                return Result<IEnumerable<LaunchDto>>.Failure(anyExists.Error);
            }

            if (anyExists.Value)
            {
                Result<IEnumerable<Launch>> cached = await _launchRepository.GetAllAsync();

                if (cached.IsFailure)
                {
                    return Result<IEnumerable<LaunchDto>>.Failure(cached.Error);
                }

                return Result<IEnumerable<LaunchDto>>.Success(cached.Value!.Select(MapToDto));
            }

            Result<IEnumerable<Launch>> fetched = await _spaceXService.GetAllLaunchesAsync();

            if (fetched.IsFailure)
            {
                return Result<IEnumerable<LaunchDto>>.Failure(fetched.Error);
            }

            Result<bool> saved = await _launchRepository.SaveAllAsync(fetched.Value!);

            if (saved.IsFailure)
            {
                return Result<IEnumerable<LaunchDto>>.Failure(saved.Error);
            }

            return Result<IEnumerable<LaunchDto>>.Success(fetched.Value!.Select(MapToDto));
        }

        public async Task<Result<LaunchDto>> GetLaunchByIdAsync(string id)
        {
            Result<bool> exists = await _launchRepository.ExistsAsync(id);

            if (exists.IsFailure)
            {
                return Result<LaunchDto>.Failure(exists.Error);
            }

            if (exists.Value)
            {
                Result<Launch> cached = await _launchRepository.GetByIdAsync(id);

                if (cached.IsFailure)
                {
                    return Result<LaunchDto>.Failure(cached.Error);
                }

                return Result<LaunchDto>.Success(MapToDto(cached.Value!));
            }

            Result<Launch> fetched = await _spaceXService.GetLaunchByIdAsync(id);

            if (fetched.IsFailure)
            {
                return Result<LaunchDto>.Failure(fetched.Error);
            }

            Result<bool> saved = await _launchRepository.SaveOneAsync(fetched.Value!);

            if (saved.IsFailure)
            {
                return Result<LaunchDto>.Failure(saved.Error);
            }

            return Result<LaunchDto>.Success(MapToDto(fetched.Value!));
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
