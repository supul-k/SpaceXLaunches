namespace SpaceXLaunches.Domain.Entities
{
    public record LaunchFailure(
        int? Id,
        string LaunchId,
        int? TimeSeconds,
        int? AltitudeKm,
        string Reason
    );
}
