using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;

namespace alealamia.Application.Services;

public class TaskNoShowService : ITaskNoShowService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskReassignmentService _reassignmentService;
    private readonly TimeProvider _timeProvider;

    public TaskNoShowService(
        ITaskRepository taskRepository,
        ITaskReassignmentService reassignmentService,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _reassignmentService = reassignmentService;
        _timeProvider = timeProvider;
    }

    public async Task<NoShowRunResult> ProcessAsync()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var candidates = await _taskRepository.GetNoShowCandidatesAsync(now);

        var processed = new List<int>();
        var failures = new List<NoShowFailure>();

        foreach (var task in candidates)
        {
            try
            {
                await _reassignmentService.RecordNoShowAsync(task.Id);
                processed.Add(task.Id);
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
            {
                // Typically the task changed state (for example the freelancer delivered) between the query and the update.
                failures.Add(new NoShowFailure(task.Id, ex.Message));
            }
        }

        return new NoShowRunResult(processed, failures);
    }
}
