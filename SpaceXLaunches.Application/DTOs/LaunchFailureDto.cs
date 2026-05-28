namespace SpaceXLaunches.Application.DTOs
{
    public record LaunchFailureDto(
        int? TimeSeconds,
        int? AltitudeKm,
        string Reason
    );
}
