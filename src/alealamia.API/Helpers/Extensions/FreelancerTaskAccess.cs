using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Enums;

namespace alealamia.API.Helpers.Extensions;

public static class FreelancerTaskAccess
{
    /// <summary>
    /// Returns the id and track of the calling freelancer. Throws
    /// UnauthorizedAccessException (403) when the profile is missing or not approved.
    /// </summary>
    public static async Task<(int Id, int TrackId)> GetApprovedFreelancerAsync(
        this IFreelancerProfileService service,
        System.Security.Claims.ClaimsPrincipal user)
    {
        var userId = user.GetUserId();

        var profile = await service.GetFreelancerProfileAsync(userId)
            ?? throw new UnauthorizedAccessException("The freelancer profile has not been completed.");

        var status = await service.GetFreelancerStatusAsync(userId);
        if (status is null || status.ApprovalStatus != FreelancerApprovalStatus.Approved)
            throw new UnauthorizedAccessException("The freelancer profile has not been approved yet.");

        return (profile.Id, profile.TrackId);
    }

    public static async Task<bool> HasOfferAsync(
        this ITaskOfferService offers,
        int taskId,
        int freelancerProfileId)
    {
        var result = await offers.SearchAsync(new TaskOfferFilterDto
        {
            TaskId = taskId,
            FreelancerProfileId = freelancerProfileId,
            Page = 1,
            PageSize = 1
        });

        return result.TotalCount > 0;
    }
}
