namespace alealamia.Application.Interfaces.Services;

public interface ITaskNoShowService
{
    /// <summary>
    /// Finds assigned tasks that passed their final deadline without a delivery and, for each one, returns the task to
    /// the pool and blocks the freelancer. A failure on one task does not stop the others.
    /// </summary>
    Task<NoShowRunResult> ProcessAsync();
}

public sealed record NoShowRunResult(IReadOnlyList<int> ProcessedTaskIds, IReadOnlyList<NoShowFailure> Failures);

public sealed record NoShowFailure(int TaskId, string Error);
