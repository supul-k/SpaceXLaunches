using Microsoft.Data.SqlClient;

namespace SpaceXLaunches.API.Extension
{
    public class EnsureSchema
    {
        public static async Task EnsureSchemaAsync(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string databaseName = builder.InitialCatalog;

            builder.InitialCatalog = "master";

            using (var masterConnection = new SqlConnection(builder.ToString()))
            {
                await masterConnection.OpenAsync();

                string checkDbQuery = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'";
                using (var checkCmd = new SqlCommand(checkDbQuery, masterConnection))
                {
                    int dbExists = (int)await checkCmd.ExecuteScalarAsync();

                    if (dbExists == 0)
                    {
                        string createDbQuery = $"CREATE DATABASE [{databaseName}]";
                        using (var createCmd = new SqlCommand(createDbQuery, masterConnection))
                        {
                            await createCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"✓ Database '{databaseName}' created successfully");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"✓ Database '{databaseName}' already exists");
                    }
                }
            }

            builder.InitialCatalog = databaseName;

            using (var connection = new SqlConnection(builder.ToString()))
            {
                await connection.OpenAsync();

                string schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.sql");
                if (!File.Exists(schemaPath))
                {
                    throw new FileNotFoundException($"schema.sql not found at: {schemaPath}");
                }

                string sql = await File.ReadAllTextAsync(schemaPath);

                var statements = sql.Split(new[] { "GO", ";\n", ";\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var statement in statements)
                {
                    if (!string.IsNullOrWhiteSpace(statement))
                    {
                        try
                        {
                            using (var command = new SqlCommand(statement, connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        catch (SqlException ex) when (ex.Number == 2714)
                        {
                            Console.WriteLine($"Table already exists, continuing...");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine("✓ Schema applied successfully");

                string verifyTablesQuery = @"
            SELECT COUNT(*) FROM sys.tables WHERE name IN ('Launches', 'LaunchFailures')";
                using (var verifyCmd = new SqlCommand(verifyTablesQuery, connection))
                {
                    int tableCount = (int)await verifyCmd.ExecuteScalarAsync();
                    Console.WriteLine($"✓ Found {tableCount}/2 tables");
                }
            }
        }
    }
}
