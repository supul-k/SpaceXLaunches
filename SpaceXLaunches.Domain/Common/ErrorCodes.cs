namespace SpaceXLaunches.Domain.Common
{
    public static class ErrorCodes
    {
        public const string SpaceXApiUnavailable = "SPACEX_API_UNAVAILABLE";
        public const string SpaceXApiNotFound = "SPACEX_LAUNCH_NOT_FOUND";
        public const string SpaceXApiParseFailure = "SPACEX_API_PARSE_FAILURE";

        public const string DatabaseReadFailure = "DATABASE_READ_FAILURE";
        public const string DatabaseWriteFailure = "DATABASE_WRITE_FAILURE";

        public const string InvalidLaunchId = "INVALID_LAUNCH_ID";
    }
}
