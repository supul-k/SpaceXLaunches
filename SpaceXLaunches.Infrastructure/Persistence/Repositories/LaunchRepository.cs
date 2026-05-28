using Microsoft.Data.SqlClient;
using SpaceXLaunches.Domain.Common;
using SpaceXLaunches.Domain.Entities;
using SpaceXLaunches.Domain.Interfaces;

namespace SpaceXLaunches.Infrastructure.Persistence.Repositories
{
    public class LaunchRepository : ILaunchRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        public LaunchRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Result<bool>> AnyExistsAsync()
        {
            const string sql = "SELECT TOP 1 1 FROM Launches";

            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(sql, connection);
                object? result = await command.ExecuteScalarAsync();
                return Result<bool>.Success(result is not null);
            }
            catch (SqlException ex)
            {
                return Result<bool>.Failure(
                    ErrorCodes.DatabaseReadFailure,
                    $"Failed to check if launches exist: {ex.Message}"
                );
            }
        }

        public async Task<Result<bool>> ExistsAsync(string id)
        {
            const string sql = "SELECT TOP 1 1 FROM Launches WHERE Id = @Id";

            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                object? result = await command.ExecuteScalarAsync();
                return Result<bool>.Success(result is not null);
            }
            catch (SqlException ex)
            {
                return Result<bool>.Failure(
                    ErrorCodes.DatabaseReadFailure,
                    $"Failed to check existence of launch '{id}': {ex.Message}"
                );
            }
        }

        public async Task<Result<IEnumerable<Launch>>> GetAllAsync()
        {
            const string sql = @"
            SELECT
                l.Id, l.FlightNumber, l.Name, l.DateUtc, l.Success,
                l.Details, l.RocketId, l.PatchSmall, l.PatchLarge,
                l.Webcast, l.Article, l.Wikipedia,
                f.Id        AS FailureId,
                f.TimeSeconds, f.AltitudeKm, f.Reason, f.LaunchId
            FROM Launches l
            LEFT JOIN LaunchFailures f ON f.LaunchId = l.Id
            ORDER BY l.FlightNumber";

            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(sql, connection);
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                IEnumerable<Launch> launches = await ReadLaunchesFromReader(reader);
                return Result<IEnumerable<Launch>>.Success(launches);
            }
            catch (SqlException ex)
            {
                return Result<IEnumerable<Launch>>.Failure(
                    ErrorCodes.DatabaseReadFailure,
                    $"Failed to retrieve launches from database: {ex.Message}"
                );
            }
        }

        public async Task<Result<Launch>> GetByIdAsync(string id)
        {
            const string sql = @"
            SELECT
                l.Id, l.FlightNumber, l.Name, l.DateUtc, l.Success,
                l.Details, l.RocketId, l.PatchSmall, l.PatchLarge,
                l.Webcast, l.Article, l.Wikipedia,
                f.Id        AS FailureId,
                f.TimeSeconds, f.AltitudeKm, f.Reason, f.LaunchId
            FROM Launches l
            LEFT JOIN LaunchFailures f ON f.LaunchId = l.Id
            WHERE l.Id = @Id";

            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                IEnumerable<Launch> results = await ReadLaunchesFromReader(reader);
                Launch? launch = results.FirstOrDefault();

                if (launch is null)
                {
                    return Result<Launch>.Failure(
                        ErrorCodes.SpaceXApiNotFound,
                        $"Launch with id '{id}' was not found in the database."
                    );
                }

                return Result<Launch>.Success(launch);
            }
            catch (SqlException ex)
            {
                return Result<Launch>.Failure(
                    ErrorCodes.DatabaseReadFailure,
                    $"Failed to retrieve launch '{id}' from database: {ex.Message}"
                );
            }
        }

        public async Task<Result<bool>> SaveOneAsync(Launch launch)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    await InsertLaunchAsync(launch, connection, transaction);

                    foreach (LaunchFailure failure in launch.Failures)
                    {
                        await InsertFailureAsync(failure, launch.Id, connection, transaction);
                    }

                    await transaction.CommitAsync();
                    return Result<bool>.Success(true);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (SqlException ex)
            {
                return Result<bool>.Failure(
                    ErrorCodes.DatabaseWriteFailure,
                    $"Failed to save launch '{launch.Id}': {ex.Message}"
                );
            }
        }

        public async Task<Result<bool>> SaveAllAsync(IEnumerable<Launch> launches)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                using SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    foreach (Launch launch in launches)
                    {
                        await InsertLaunchAsync(launch, connection, transaction);

                        foreach (LaunchFailure failure in launch.Failures)
                        {
                            await InsertFailureAsync(failure, launch.Id, connection, transaction);
                        }
                    }

                    await transaction.CommitAsync();
                    return Result<bool>.Success(true);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (SqlException ex)
            {
                return Result<bool>.Failure(
                    ErrorCodes.DatabaseWriteFailure,
                    $"Failed to save launches to database: {ex.Message}"
                );
            }
        }

        private static async Task<IEnumerable<Launch>> ReadLaunchesFromReader(SqlDataReader reader)
        {
            Dictionary<string, (Launch launch, List<LaunchFailure> failures)> map = new();

            while (await reader.ReadAsync())
            {
                string id = reader.GetString(reader.GetOrdinal("Id"));

                if (!map.ContainsKey(id))
                {
                    Launch launch = new Launch(
                        Id: id,
                        FlightNumber: reader.GetInt32(reader.GetOrdinal("FlightNumber")),
                        Name: reader.GetString(reader.GetOrdinal("Name")),
                        DateUtc: reader.GetDateTime(reader.GetOrdinal("DateUtc")),
                        Success: reader.IsDBNull(reader.GetOrdinal("Success"))
                                          ? null
                                          : reader.GetBoolean(reader.GetOrdinal("Success")),
                        Details: reader.IsDBNull(reader.GetOrdinal("Details"))
                                          ? null
                                          : reader.GetString(reader.GetOrdinal("Details")),
                        RocketId: reader.IsDBNull(reader.GetOrdinal("RocketId"))
                                          ? null
                                          : reader.GetString(reader.GetOrdinal("RocketId")),
                        PatchSmall: reader.IsDBNull(reader.GetOrdinal("PatchSmall"))
                                          ? null
                                          : reader.GetString(reader.GetOrdinal("PatchSmall")),
                        PatchLarge: reader.IsDBNull(reader.GetOrdinal("PatchLarge"))
                                          ? null
                                          : reader.GetString(reader.GetOrdinal("PatchLarge")),
                        Webcast: reader.IsDBNull(reader.GetOrdinal("Webcast"))
                                          ? null
                                          : reader.GetString(reader.GetOrdinal("Webcast")),
                        Article: reader.IsDBNull(reader.GetOrdinal("Article"))
                                          ? null
                                          : reader.GetString(reader.GetOrdinal("Article")),
                        Wikipedia: reader.IsDBNull(reader.GetOrdinal("Wikipedia"))
                                          ? null
                                          : reader.GetString(reader.GetOrdinal("Wikipedia")),
                        Failures: new List<LaunchFailure>()
                    );

                    map[id] = (launch, new List<LaunchFailure>());
                }

                if (!reader.IsDBNull(reader.GetOrdinal("FailureId")))
                {
                    LaunchFailure failure = new LaunchFailure(
                        Id: reader.GetInt32(reader.GetOrdinal("FailureId")),
                        LaunchId: id,
                        TimeSeconds: reader.IsDBNull(reader.GetOrdinal("TimeSeconds"))
                                         ? null
                                         : reader.GetInt32(reader.GetOrdinal("TimeSeconds")),
                        AltitudeKm: reader.IsDBNull(reader.GetOrdinal("AltitudeKm"))
                                         ? null
                                         : reader.GetInt32(reader.GetOrdinal("AltitudeKm")),
                        Reason: reader.GetString(reader.GetOrdinal("Reason"))
                    );

                    map[id].failures.Add(failure);
                }
            }

            return map.Values.Select(entry =>
                entry.launch with { Failures = entry.failures.AsReadOnly() }
            );
        }

        private static async Task InsertLaunchAsync(
            Launch launch,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string sql = @"
            INSERT INTO Launches
                (Id, FlightNumber, Name, DateUtc, Success, Details,
                 RocketId, PatchSmall, PatchLarge, Webcast, Article, Wikipedia)
            VALUES
                (@Id, @FlightNumber, @Name, @DateUtc, @Success, @Details,
                 @RocketId, @PatchSmall, @PatchLarge, @Webcast, @Article, @Wikipedia)";

            using SqlCommand command = new SqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("@Id", launch.Id);
            command.Parameters.AddWithValue("@FlightNumber", launch.FlightNumber);
            command.Parameters.AddWithValue("@Name", launch.Name);
            command.Parameters.AddWithValue("@DateUtc", launch.DateUtc);
            command.Parameters.AddWithValue("@Success", (object?)launch.Success ?? DBNull.Value);
            command.Parameters.AddWithValue("@Details", (object?)launch.Details ?? DBNull.Value);
            command.Parameters.AddWithValue("@RocketId", (object?)launch.RocketId ?? DBNull.Value);
            command.Parameters.AddWithValue("@PatchSmall", (object?)launch.PatchSmall ?? DBNull.Value);
            command.Parameters.AddWithValue("@PatchLarge", (object?)launch.PatchLarge ?? DBNull.Value);
            command.Parameters.AddWithValue("@Webcast", (object?)launch.Webcast ?? DBNull.Value);
            command.Parameters.AddWithValue("@Article", (object?)launch.Article ?? DBNull.Value);
            command.Parameters.AddWithValue("@Wikipedia", (object?)launch.Wikipedia ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        private static async Task InsertFailureAsync(
            LaunchFailure failure,
            string launchId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string sql = @"
            INSERT INTO LaunchFailures (LaunchId, TimeSeconds, AltitudeKm, Reason)
            VALUES (@LaunchId, @TimeSeconds, @AltitudeKm, @Reason)";

            using SqlCommand command = new SqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("@LaunchId", launchId);
            command.Parameters.AddWithValue("@TimeSeconds", (object?)failure.TimeSeconds ?? DBNull.Value);
            command.Parameters.AddWithValue("@AltitudeKm", (object?)failure.AltitudeKm ?? DBNull.Value);
            command.Parameters.AddWithValue("@Reason", failure.Reason);

            await command.ExecuteNonQueryAsync();
        }
    }
}
