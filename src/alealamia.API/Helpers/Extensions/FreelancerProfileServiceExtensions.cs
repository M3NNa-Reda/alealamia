using System.Security.Claims;
using alealamia.Application.Interfaces.Services;

namespace alealamia.API.Helpers.Extensions;

public static class FreelancerProfileServiceExtensions
{
    public static async Task<int> GetProfileIdAsync(this IFreelancerProfileService service, ClaimsPrincipal user)
    {
        var profile = await service.GetFreelancerProfileAsync(user.GetUserId());
        if (profile is null)
            throw new UnauthorizedAccessException("The freelancer profile has not been completed.");

        return profile.Id;
    }
}
