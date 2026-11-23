using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using Microsoft.Extensions.DependencyInjection;
namespace alealamia.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFreelancerProfileService, FreelancerProfileService>();
            services.AddScoped<IAdminFreelancerService, AdminFreelancerService>();
            services.AddScoped<ITrackService, TrackService>();
            services.AddScoped<ISkillService, SkillService>();
            return services;
        }
    }
}
