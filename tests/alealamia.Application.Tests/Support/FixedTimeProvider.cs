namespace alealamia.Application.Tests.Support;

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTime utcNow)
    {
        _now = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
    }

    public override DateTimeOffset GetUtcNow() => _now;
}
