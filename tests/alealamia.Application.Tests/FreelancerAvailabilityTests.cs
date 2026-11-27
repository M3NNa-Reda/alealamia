using alealamia.Application.DTOs.Freelancer;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using NSubstitute;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Tests;

/// <summary>Availability: approval switches it on, only approved freelancers can change it.</summary>
public class FreelancerAvailabilityTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IFreelancerProfileRepository _profiles = Substitute.For<IFreelancerProfileRepository>();
    private readonly FreelancerProfileService _profileService;
    private readonly AdminFreelancerService _adminService;

    public FreelancerAvailabilityTests()
    {
        _uow.FreelancerProfiles.Returns(_profiles);
        _profileService = new FreelancerProfileService(_uow, Substitute.For<IIdentityService>(), Substitute.For<ITaskRepository>());
        _adminService = new AdminFreelancerService(_uow, Substitute.For<IIdentityService>());
    }

    private static FreelancerProfile Profile(FreelancerApprovalStatus status, bool available) => new()
    {
        Id = 5,
        UserId = "u-5",
        ApprovalStatus = status,
        IsAvailable = available
    };

    [Fact]
    public async Task Approve_SwitchesAvailabilityOn()
    {
        var profile = Profile(FreelancerApprovalStatus.Pending, available: false);
        _profiles.GetByIdAsync(5).Returns(profile);

        var result = await _adminService.ApproveFreelancerAsync(5);

        Assert.True(result.Succeeded);
        Assert.Equal(FreelancerApprovalStatus.Approved, profile.ApprovalStatus);
        Assert.True(profile.IsAvailable);
    }

    [Fact]
    public async Task Reject_KeepsAvailabilityOff()
    {
        var profile = Profile(FreelancerApprovalStatus.Pending, available: false);
        _profiles.GetByIdAsync(5).Returns(profile);

        var result = await _adminService.RejectFreelancerAsync(5);

        Assert.True(result.Succeeded);
        Assert.False(profile.IsAvailable);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task SetAvailability_ApprovedFreelancer_UpdatesTheFlag(bool before, bool requested)
    {
        var profile = Profile(FreelancerApprovalStatus.Approved, before);
        _profiles.GetByUserIdAsync("u-5").Returns(profile);

        var result = await _profileService.SetAvailabilityAsync("u-5", new SetAvailabilityDto { IsAvailable = requested });

        Assert.True(result.Succeeded);
        Assert.Equal(requested, profile.IsAvailable);
        await _uow.Received(1).CompleteAsync();
    }

    [Theory]
    [InlineData(FreelancerApprovalStatus.Pending)]
    [InlineData(FreelancerApprovalStatus.Rejected)]
    public async Task SetAvailability_NotApproved_IsRefused(FreelancerApprovalStatus status)
    {
        var profile = Profile(status, available: false);
        _profiles.GetByUserIdAsync("u-5").Returns(profile);

        var result = await _profileService.SetAvailabilityAsync("u-5", new SetAvailabilityDto { IsAvailable = true });

        Assert.False(result.Succeeded);
        Assert.False(profile.IsAvailable);
        await _uow.DidNotReceive().CompleteAsync();
    }

    [Fact]
    public async Task SetAvailability_WithoutAProfile_Fails()
    {
        _profiles.GetByUserIdAsync("nobody").Returns((FreelancerProfile?)null);

        var result = await _profileService.SetAvailabilityAsync("nobody", new SetAvailabilityDto { IsAvailable = true });

        Assert.False(result.Succeeded);
    }
}
