namespace SpaceXLaunches.Application.Services;

using SpaceXLaunches.Application.DTOs;
using SpaceXLaunches.Domain.Common;
using SpaceXLaunches.Domain.Entities;
using SpaceXLaunches.Domain.Interfaces;

public class LaunchService
{
    private readonly ILaunchRepository _repository;
    private readonly ISpaceXService _spaceXService;

    public LaunchService(ILaunchRepository repository, ISpaceXService spaceXService)
    {
        _repository = repository;
        _spaceXService = spaceXService;
    }

    public async Task<Result<PagedResultDto<LaunchDto>>> GetAllLaunchesAsync(PaginationParams pagination)
    {
        Result<bool> anyExists = await _repository.AnyExistsAsync();

        if (anyExists.IsFailure)
        {
            return Result<PagedResultDto<LaunchDto>>.Failure(anyExists.Error);
        }

        if (!anyExists.Value)
        {
            Result<IEnumerable<Launch>> fetched = await _spaceXService.GetAllLaunchesAsync();

            if (fetched.IsFailure)
            {
                return Result<PagedResultDto<LaunchDto>>.Failure(fetched.Error);
            }

            Result<bool> saved = await _repository.SaveAllAsync(fetched.Value!);

            if (saved.IsFailure)
            {
                return Result<PagedResultDto<LaunchDto>>.Failure(saved.Error);
            }
        }

        Result<int> countResult = await _repository.GetTotalCountAsync();

        if (countResult.IsFailure)
        {
            return Result<PagedResultDto<LaunchDto>>.Failure(countResult.Error);
        }

        Result<IEnumerable<Launch>> paged = await _repository.GetAllPagedAsync(pagination);

        if (paged.IsFailure)
        {
            return Result<PagedResultDto<LaunchDto>>.Failure(paged.Error);
        }

        int totalCount = countResult.Value;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize);

        PagedResultDto<LaunchDto> result = new PagedResultDto<LaunchDto>(
            Page: pagination.Page,
            PageSize: pagination.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            Data: paged.Value!.Select(MapToDto)
        );

        return Result<PagedResultDto<LaunchDto>>.Success(result);
    }

    public async Task<Result<LaunchDto>> GetLaunchByIdAsync(string id)
    {
        Result<bool> exists = await _repository.ExistsAsync(id);

        if (exists.IsFailure)
        {
            return Result<LaunchDto>.Failure(exists.Error);
        }

        if (exists.Value)
        {
            Result<Launch> cached = await _repository.GetByIdAsync(id);

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

        Result<bool> saved = await _repository.SaveOneAsync(fetched.Value!);

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