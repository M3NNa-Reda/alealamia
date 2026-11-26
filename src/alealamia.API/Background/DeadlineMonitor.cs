using alealamia.Application.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace alealamia.API.Background
{
    public sealed class DeadlineMonitorOptions
    {
        public const string SectionName = "DeadlineMonitor";

        public bool Enabled { get; set; } = true;

        public int IntervalMinutes { get; set; } = 5;
    }

    /// <summary>
    /// Every few minutes returns the tasks whose freelancer missed the final deadline to the pool and blocks that
    /// freelancer (BR-09). The first run happens one interval after startup.
    /// </summary>
    public sealed class DeadlineMonitor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IOptions<DeadlineMonitorOptions> _options;
        private readonly ILogger<DeadlineMonitor> _logger;

        public DeadlineMonitor(
            IServiceScopeFactory scopes,
            IOptions<DeadlineMonitorOptions> options,
            ILogger<DeadlineMonitor> logger)
        {
            _scopes = scopes;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = _options.Value;
            if (!settings.Enabled)
            {
                _logger.LogInformation("Deadline monitor is disabled.");
                return;
            }

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, settings.IntervalMinutes)));

            while (await WaitAsync(timer, stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITaskNoShowService>();
                    var result = await service.ProcessAsync();

                    if (result.ProcessedTaskIds.Count > 0)
                        _logger.LogInformation("Deadline monitor returned {Count} task(s) to the pool.", result.ProcessedTaskIds.Count);

                    foreach (var failure in result.Failures)
                        _logger.LogWarning("Deadline monitor skipped task {TaskId}: {Error}", failure.TaskId, failure.Error);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Deadline monitor run failed.");
                }
            }
        }

        private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
        {
            try
            {
                return await timer.WaitForNextTickAsync(token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
