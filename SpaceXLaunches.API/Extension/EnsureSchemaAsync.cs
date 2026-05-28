using Microsoft.Data.SqlClient;

namespace SpaceXLaunches.API.Extension;

public class EnsureSchema
{
    public static async Task EnsureSchemaAsync(string connectionString)
    {
        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
        string databaseName = builder.InitialCatalog;

        builder.InitialCatalog = "master";

        using (SqlConnection masterConnection = new SqlConnection(builder.ToString()))
        {
            await masterConnection.OpenAsync();

            string checkDbSql = "SELECT COUNT(*) FROM sys.databases WHERE name = @DbName";

            using (SqlCommand checkCmd = new SqlCommand(checkDbSql, masterConnection))
            {
                checkCmd.Parameters.AddWithValue("@DbName", databaseName);
                int dbExists = (int)(await checkCmd.ExecuteScalarAsync())!;

                if (dbExists == 0)
                {
                    string createDbSql = $"CREATE DATABASE [{databaseName.Replace("]", "]]")}]";

                    using (SqlCommand createCmd = new SqlCommand(createDbSql, masterConnection))
                    {
                        await createCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"✓ Database '{databaseName}' created");
                    }
                }
                else
                {
                    Console.WriteLine($"✓ Database '{databaseName}' already exists");
                }
            }
        }

        builder.InitialCatalog = databaseName;

        using (SqlConnection connection = new SqlConnection(builder.ToString()))
        {
            await connection.OpenAsync();

            string schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.sql");

            if (!File.Exists(schemaPath))
            {
                throw new FileNotFoundException($"schema.sql not found at: {schemaPath}");
            }

            string fullSql = await File.ReadAllTextAsync(schemaPath);

            string[] batches = System.Text.RegularExpressions.Regex
                .Split(fullSql, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline
                                              | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (string batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;

                using (SqlCommand command = new SqlCommand(batch, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            Console.WriteLine("✓ Schema applied successfully");

            string verifySql = "SELECT COUNT(*) FROM sys.tables WHERE name IN ('Launches', 'LaunchFailures')";

            using (SqlCommand verifyCmd = new SqlCommand(verifySql, connection))
            {
                int tableCount = (int)(await verifyCmd.ExecuteScalarAsync())!;
                Console.WriteLine($"✓ Found {tableCount}/2 tables");
            }
        }
    }
}