using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.DTOs.Tasks;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using alealamia.Application.Tests.Support;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using NSubstitute;
using FileEntity = alealamia.Domain.Entities.File;
using Task = System.Threading.Tasks.Task;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Tests;

public class TaskServiceTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly ITaskRequiredSkillRepository _requiredSkills = Substitute.For<ITaskRequiredSkillRepository>();
    private readonly ITaskStatusHistoryRepository _history = Substitute.For<ITaskStatusHistoryRepository>();
    private readonly ITaskFileRepository _taskFiles = Substitute.For<ITaskFileRepository>();
    private readonly ITaskOfferRepository _offers = Substitute.For<ITaskOfferRepository>();
    private readonly ITaskOfferService _offerService = Substitute.For<ITaskOfferService>();
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IGenericRepository<Track> _tracks = Substitute.For<IGenericRepository<Track>>();
    private readonly IGenericRepository<Skill> _skills = Substitute.For<IGenericRepository<Skill>>();
    private readonly IGenericRepository<FileEntity> _files = Substitute.For<IGenericRepository<FileEntity>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _projects.GetByIdAsync(10).Returns(new Project { Id = 10, Status = ProjectStatus.PriceSet });
        _tracks.GetByIdAsync(3).Returns(new Track { Id = 3, IsActive = true });
        _skills.GetByIdAsync(1).Returns(new Skill { Id = 1, IsActive = true });
        _skills.GetByIdAsync(2).Returns(new Skill { Id = 2, IsActive = true });

        _service = new TaskService(
            _tasks,
            _requiredSkills,
            _history,
            _taskFiles,
            _offers,
            _offerService,
            _projects,
            _tracks,
            _skills,
            _files,
            _unitOfWork,
            new FixedTimeProvider(TestData.Now));
    }

    private static CreateTaskDto NewCreateDto() => new()
    {
        ProjectId = 10,
        TrackId = 3,
        Title = "  Design the landing page  ",
        Description = "Create the landing page layout.",
        RewardAmount = 1500.50m,
        PublishedDeadline = TestData.Now.AddDays(7),
        RequiredSkillIds = new List<int> { 1, 2 },
        CreatedByUserId = "admin-1"
    };

    private static UpdateTaskStatusDto StatusChange(TaskStatus status) => new()
    {
        NewStatus = status,
        ChangedByUserId = "admin-1"
    };

    [Fact]
    public async Task Create_StoresADraftWithItsSkillsAndHistory()
    {
        var result = await _service.CreateAsync(NewCreateDto());

        Assert.Equal(TaskStatus.Draft, result.Status);
        Assert.Equal("Design the landing page", result.Title);
        Assert.Equal(1500.50m, result.RewardAmount);
        await _tasks.Received(1).AddAsync(Arg.Is<TaskEntity>(t =>
            t.Status == TaskStatus.Draft
            && t.RewardAmount == 1500.50m
            && t.TaskRequiredSkills.Count == 2
            && t.TaskStatusHistories.Count == 1
            && t.CreatedAt == TestData.Now));
        await _unitOfWork.Received(1).CompleteAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Pending)]
    [InlineData(ProjectStatus.UnderReview)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Cancelled)]
    public async Task Create_Throws_WhenTheProjectPriceIsNotApproved(ProjectStatus status)
    {
        _projects.GetByIdAsync(10).Returns(new Project { Id = 10, Status = status });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(NewCreateDto()));

        await _tasks.DidNotReceive().AddAsync(Arg.Any<TaskEntity>());
    }

    [Fact]
    public async Task Create_Throws_WhenTheProjectDoesNotExist()
    {
        _projects.GetByIdAsync(10).Returns((Project?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.CreateAsync(NewCreateDto()));
    }

    [Fact]
    public async Task Create_Throws_WhenTheTrackIsInactive()
    {
        _tracks.GetByIdAsync(3).Returns(new Track { Id = 3, IsActive = false });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(NewCreateDto()));
    }

    [Fact]
    public async Task Create_Throws_WhenASkillIsInactive()
    {
        _skills.GetByIdAsync(2).Returns(new Skill { Id = 2, IsActive = false });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(NewCreateDto()));
    }

    [Fact]
    public async Task Create_Throws_WhenASkillIsSelectedTwice()
    {
        var dto = NewCreateDto();
        dto.RequiredSkillIds = new List<int> { 1, 1 };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_Throws_WhenTheDeadlineIsInThePast()
    {
        var dto = NewCreateDto();
        dto.PublishedDeadline = TestData.Now.AddMinutes(-1);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_Throws_WhenTheTitleIsTooLong()
    {
        var dto = NewCreateDto();
        dto.Title = new string('a', 201);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10.555)]
    public async Task Create_Throws_WhenTheRewardAmountIsInvalid(double rewardAmount)
    {
        var dto = NewCreateDto();
        dto.RewardAmount = (decimal)rewardAmount;

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        await _tasks.DidNotReceive().AddAsync(Arg.Any<TaskEntity>());
    }

    [Fact]
    public async Task Create_Throws_WhenTheRewardAmountExceedsTheMaximum()
    {
        var dto = NewCreateDto();
        dto.RewardAmount = alealamia.Domain.Constants.TaskRules.MaxRewardAmount + 1;

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task Update_Throws_WhenTheRewardAmountIsInvalid()
    {
        _tasks.GetByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Draft));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, new UpdateTaskDto
        {
            Title = "Title",
            Description = "Description",
            RewardAmount = 0,
            PublishedDeadline = TestData.Now.AddDays(2)
        }));
    }

    [Fact]
    public async Task Update_Throws_WhenTheTaskIsNotADraft()
    {
        _tasks.GetByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Published));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(1, new UpdateTaskDto
        {
            Title = "Title",
            Description = "Description",
            RewardAmount = 100m,
            PublishedDeadline = TestData.Now.AddDays(2)
        }));
    }

    [Fact]
    public async Task Update_ChangesADraft()
    {
        var task = TestData.NewTask(status: TaskStatus.Draft);
        _tasks.GetByIdAsync(1).Returns(task);

        var result = await _service.UpdateAsync(1, new UpdateTaskDto
        {
            Title = "New title",
            Description = "New description",
            RewardAmount = 2750.25m,
            PublishedDeadline = TestData.Now.AddDays(2)
        });

        Assert.Equal("New title", result.Title);
        Assert.Equal(2750.25m, result.RewardAmount);
        Assert.Equal(TestData.Now, result.UpdatedAt);
        await _unitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task UpdateStatus_Publish_SendsOffersInsideTheTransaction()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Draft));
        _tasks.TryChangeStatusAsync(1, TaskStatus.Draft, TaskStatus.Published, TestData.Now).Returns(true);

        var result = await _service.UpdateStatusAsync(1, StatusChange(TaskStatus.Published));

        Assert.Equal(TaskStatus.Published, result.Status);
        await _offerService.Received(1).SendOffersAsync(1);
        await _history.Received(1).AddAsync(Arg.Is<TaskStatusHistory>(h =>
            h.OldStatus == TaskStatus.Draft && h.NewStatus == TaskStatus.Published));
        await _unitOfWork.Received(1).CommitTransactionAsync();
    }

    [Fact]
    public async Task UpdateStatus_Cancel_ClosesThePendingOffers()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Published));
        _tasks.TryChangeStatusAsync(1, TaskStatus.Published, TaskStatus.Cancelled, TestData.Now).Returns(true);

        await _service.UpdateStatusAsync(1, StatusChange(TaskStatus.Cancelled));

        await _offers.Received(1).ClosePendingAsync(1, null, TestData.Now);
        await _offerService.DidNotReceive().SendOffersAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task UpdateStatus_RollsBack_WhenAnotherUserChangedTheStatus()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Draft));
        _tasks.TryChangeStatusAsync(1, TaskStatus.Draft, TaskStatus.Published, TestData.Now).Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateStatusAsync(1, StatusChange(TaskStatus.Published)));

        await _unitOfWork.Received(1).RollbackTransactionAsync();
        await _unitOfWork.DidNotReceive().CommitTransactionAsync();
        await _history.DidNotReceive().AddAsync(Arg.Any<TaskStatusHistory>());
    }

    [Fact]
    public async Task UpdateStatus_RollsBack_WhenSendingOffersFails()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Draft));
        _tasks.TryChangeStatusAsync(1, TaskStatus.Draft, TaskStatus.Published, TestData.Now).Returns(true);
        _offerService.SendOffersAsync(1)
            .Returns(Task.FromException<IEnumerable<TaskOfferDto>>(new InvalidOperationException("Offers failed.")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateStatusAsync(1, StatusChange(TaskStatus.Published)));

        await _unitOfWork.Received(1).RollbackTransactionAsync();
        await _unitOfWork.DidNotReceive().CommitTransactionAsync();
    }

    [Theory]
    [InlineData(TaskStatus.Draft, TaskStatus.Completed)]
    [InlineData(TaskStatus.Draft, TaskStatus.Assigned)]
    [InlineData(TaskStatus.Published, TaskStatus.Assigned)]
    [InlineData(TaskStatus.Published, TaskStatus.Completed)]
    [InlineData(TaskStatus.Assigned, TaskStatus.Completed)]
    [InlineData(TaskStatus.Completed, TaskStatus.Draft)]
    [InlineData(TaskStatus.Cancelled, TaskStatus.Published)]
    public async Task UpdateStatus_RejectsInvalidTransitions(TaskStatus from, TaskStatus to)
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: from));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateStatusAsync(1, StatusChange(to)));

        await _tasks.DidNotReceive().TryChangeStatusAsync(
            Arg.Any<int>(), Arg.Any<TaskStatus>(), Arg.Any<TaskStatus>(), Arg.Any<DateTime>());
    }

    [Theory]
    [InlineData(TaskStatus.Assigned, TaskStatus.Submitted)]
    [InlineData(TaskStatus.Submitted, TaskStatus.RevisionRequested)]
    [InlineData(TaskStatus.Submitted, TaskStatus.Completed)]
    [InlineData(TaskStatus.RevisionRequested, TaskStatus.Submitted)]
    public async Task UpdateStatus_AllowsTheDeliveryFlow(TaskStatus from, TaskStatus to)
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: from, assignedFreelancerId: 5));
        _tasks.TryChangeStatusAsync(1, from, to, TestData.Now).Returns(true);

        var result = await _service.UpdateStatusAsync(1, StatusChange(to));

        Assert.Equal(to, result.Status);
    }

    [Fact]
    public async Task UpdateStatus_Publish_Throws_WhenTheDeadlineHasPassed()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Draft, deadline: TestData.Now.AddHours(-1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateStatusAsync(1, StatusChange(TaskStatus.Published)));
    }

    [Fact]
    public async Task UpdateStatus_RequiresTheUserId()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateStatusAsync(1, new UpdateTaskStatusDto
        {
            NewStatus = TaskStatus.Published
        }));
    }

    [Fact]
    public async Task UpdateRequiredSkills_AddsTheNewOnesAndRemovesTheOldOnes()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Draft));
        _requiredSkills.GetByTaskIdAsync(1).Returns(new List<TaskRequiredSkill>
        {
            new() { TaskId = 1, SkillId = 1 },
            new() { TaskId = 1, SkillId = 9 }
        });

        await _service.UpdateRequiredSkillsAsync(1, new UpdateTaskRequiredSkillsDto { SkillIds = new List<int> { 1, 2 } });

        _requiredSkills.Received(1).DeleteRange(Arg.Is<IEnumerable<TaskRequiredSkill>>(s =>
            s.Count() == 1 && s.Single().SkillId == 9));
        await _requiredSkills.Received(1).AddRangeAsync(Arg.Is<IEnumerable<TaskRequiredSkill>>(s =>
            s.Count() == 1 && s.Single().SkillId == 2));
    }

    [Fact]
    public async Task UpdateRequiredSkills_Throws_WhenTheTaskIsPublished()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: TaskStatus.Published));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateRequiredSkillsAsync(1, new UpdateTaskRequiredSkillsDto { SkillIds = new List<int> { 1 } }));
    }

    [Fact]
    public async Task AddFile_Throws_WhenTheFileIsAlreadyAttached()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask());
        _files.GetByIdAsync(4).Returns(new FileEntity { Id = 4 });
        _taskFiles.GetAsync(1, 4).Returns(new TaskFile { TaskId = 1, FileId = 4 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddFileAsync(1, new AddTaskFileDto { FileId = 4 }));
    }

    [Fact]
    public async Task AddFile_AttachesTheFile()
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask());
        _files.GetByIdAsync(4).Returns(new FileEntity { Id = 4, OriginalFileName = "brief.pdf", FileUrl = "/files/brief.pdf" });
        _taskFiles.GetAsync(1, 4).Returns((TaskFile?)null);

        var result = await _service.AddFileAsync(1, new AddTaskFileDto { FileId = 4 });

        Assert.Equal("brief.pdf", result.OriginalFileName);
        await _taskFiles.Received(1).AddAsync(Arg.Is<TaskFile>(f => f.TaskId == 1 && f.FileId == 4));
        await _unitOfWork.Received(1).CompleteAsync();
    }

    [Theory]
    [InlineData(TaskStatus.Completed)]
    [InlineData(TaskStatus.Cancelled)]
    public async Task AddFile_Throws_WhenTheTaskIsClosed(TaskStatus status)
    {
        _tasks.GetReadOnlyByIdAsync(1).Returns(TestData.NewTask(status: status));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddFileAsync(1, new AddTaskFileDto { FileId = 4 }));
    }

    [Fact]
    public async Task RemoveFile_Throws_WhenTheFileIsNotAttached()
    {
        _taskFiles.GetAsync(1, 4).Returns((TaskFile?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.RemoveFileAsync(1, 4));
    }

    [Fact]
    public async Task Search_NormalizesThePagingValues()
    {
        _tasks.SearchAsync(null, null, null, null, 1, 100)
            .Returns((Items: (IReadOnlyList<TaskEntity>)new List<TaskEntity> { TestData.NewTask() }, TotalCount: 1));

        PagedResultDto<TaskDto> result = await _service.SearchAsync(new TaskFilterDto { Page = 0, PageSize = 5000 });

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
