using SpaceXLaunches.Domain.Common;
using SpaceXLaunches.Domain.Entities;
using SpaceXLaunches.Domain.Interfaces;
using System.Text.Json.Nodes;

namespace SpaceXLaunches.Infrastructure.ExternalServices
{
    public class SpaceXApiService : ISpaceXService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public SpaceXApiService(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl;
        }

        public async Task<Result<IEnumerable<Launch>>> GetAllLaunchesAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/launches");
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                JsonArray? array = JsonNode.Parse(json)?.AsArray();

                if (array is null)
                {
                    return Result<IEnumerable<Launch>>.Failure(
                        ErrorCodes.SpaceXApiParseFailure,
                        "SpaceX API returned a response that could not be parsed as a JSON array."
                    );
                }

                List<Launch> launches = new();

                foreach (JsonNode? node in array)
                {
                    if (node is null) continue;

                    Launch? launch = ParseLaunch(node);
                    if (launch is not null)
                    {
                        launches.Add(launch);
                    }
                }

                return Result<IEnumerable<Launch>>.Success(launches);
            }
            catch (HttpRequestException ex)
            {
                return Result<IEnumerable<Launch>>.Failure(
                    ErrorCodes.SpaceXApiUnavailable,
                    $"Could not reach the SpaceX API: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<Launch>>.Failure(
                    ErrorCodes.SpaceXApiParseFailure,
                    $"Unexpected error while fetching all launches: {ex.Message}"
                );
            }
        }

        public async Task<Result<Launch>> GetLaunchByIdAsync(string id)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync($"{_baseUrl}/launches/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Result<Launch>.Failure(
                        ErrorCodes.SpaceXApiNotFound,
                        $"SpaceX API returned 404 for launch id '{id}'."
                    );
                }

                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                JsonNode? node = JsonNode.Parse(json);

                if (node is null)
                {
                    return Result<Launch>.Failure(
                        ErrorCodes.SpaceXApiParseFailure,
                        $"SpaceX API returned an empty or unparseable response for launch '{id}'."
                    );
                }

                Launch? launch = ParseLaunch(node);

                if (launch is null)
                {
                    return Result<Launch>.Failure(
                        ErrorCodes.SpaceXApiParseFailure,
                        $"SpaceX API response for launch '{id}' was missing required fields."
                    );
                }

                return Result<Launch>.Success(launch);
            }
            catch (HttpRequestException ex)
            {
                return Result<Launch>.Failure(
                    ErrorCodes.SpaceXApiUnavailable,
                    $"Could not reach the SpaceX API: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                return Result<Launch>.Failure(
                    ErrorCodes.SpaceXApiParseFailure,
                    $"Unexpected error while fetching launch '{id}': {ex.Message}"
                );
            }
        }

        private static Launch? ParseLaunch(JsonNode node)
        {
            string? id = node["id"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(id)) return null;

            List<LaunchFailure> failures = new();
            JsonArray? failuresArray = node["failures"]?.AsArray();

            if (failuresArray is not null)
            {
                foreach (JsonNode? f in failuresArray)
                {
                    if (f is null) continue;
                    failures.Add(new LaunchFailure(
                        Id: null,
                        LaunchId: id,
                        TimeSeconds: f["time"]?.GetValue<int?>(),
                        AltitudeKm: f["altitude"]?.GetValue<int?>(),
                        Reason: f["reason"]?.GetValue<string>() ?? string.Empty
                    ));
                }
            }

            string? dateStr = node["date_utc"]?.GetValue<string>();
            DateTime dateUtc = dateStr is not null
                ? DateTime.Parse(dateStr, null, System.Globalization.DateTimeStyles.RoundtripKind)
                : DateTime.MinValue;

            return new Launch(
                Id: id,
                FlightNumber: node["flight_number"]?.GetValue<int>() ?? 0,
                Name: node["name"]?.GetValue<string>() ?? string.Empty,
                DateUtc: dateUtc,
                Success: node["success"]?.GetValue<bool?>(),
                Details: node["details"]?.GetValue<string>(),
                RocketId: node["rocket"]?.GetValue<string>(),
                PatchSmall: node["links"]?["patch"]?["small"]?.GetValue<string>(),
                PatchLarge: node["links"]?["patch"]?["large"]?.GetValue<string>(),
                Webcast: node["links"]?["webcast"]?.GetValue<string>(),
                Article: node["links"]?["article"]?.GetValue<string>(),
                Wikipedia: node["links"]?["wikipedia"]?.GetValue<string>(),
                Failures: failures.AsReadOnly()
            );
        }
    }
}
