namespace SpaceXLaunches.Domain.Entities
{
    public record Launch(
        string Id,
        int FlightNumber,
        string Name,
        DateTime DateUtc,
        bool? Success,
        string? Details,
        string? RocketId,
        string? PatchSmall,
        string? PatchLarge,
        string? Webcast,
        string? Article,
        string? Wikipedia,
        IReadOnlyList<LaunchFailure> Failures
    );
}
