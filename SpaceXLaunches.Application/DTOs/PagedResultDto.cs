namespace SpaceXLaunches.Application.DTOs
{
    public record PagedResultDto<T>(
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        IEnumerable<T> Data
    );
}
