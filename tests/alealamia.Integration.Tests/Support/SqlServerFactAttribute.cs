namespace alealamia.Integration.Tests.Support;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlServerAvailability.IsAvailable)
        {
            Skip = $"SQL Server is not reachable. Set {SqlServerAvailability.ConnectionVariable} to a connection string without a database name.";
        }
    }
}
