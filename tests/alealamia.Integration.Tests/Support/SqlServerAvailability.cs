using Microsoft.Data.SqlClient;

namespace alealamia.Integration.Tests.Support;

internal static class SqlServerAvailability
{
    public const string ConnectionVariable = "ALEALAMIA_TEST_CONNECTION";

    private const string DefaultConnection =
        "Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

    private static readonly Lazy<bool> Available = new(Probe);

    public static string BaseConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionVariable) is { Length: > 0 } value
            ? value
            : DefaultConnection;

    public static bool IsAvailable => Available.Value;

    private static bool Probe()
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(BaseConnectionString)
            {
                InitialCatalog = "master",
                ConnectTimeout = 3
            };

            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
