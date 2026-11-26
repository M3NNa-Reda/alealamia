using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using Microsoft.Extensions.DependencyInjection;
namespace alealamia.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IFreelancerProfileService, FreelancerProfileService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAdminFreelancerService, AdminFreelancerService>();
            services.AddScoped<IClientProfileService, ClientProfileService>();
            services.AddScoped<ITrackService, TrackService>();
            services.AddScoped<ISkillService, SkillService>();
            services.AddScoped<IFreelancerBlockService, FreelancerBlockService>();
            services.AddScoped<IPortfolioService, PortfolioService>();
            services.AddScoped<ITaskSubmissionService, TaskSubmissionService>();
            services.AddScoped<ITaskReviewService, TaskReviewService>();
            services.AddScoped<ITaskRevisionService, TaskRevisionService>();

            services.AddScoped<IFreelancerFinanceService, FreelancerFinanceService>();

            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ISupportTicketService, SupportTicketService>();
            services.AddScoped<ISupportMessageService, SupportMessageService>();

            services.AddScoped<IServiceManagementService, ServiceManagementService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IProjectStatusService, ProjectStatusService>();
            services.AddScoped<IMeetingService, MeetingService>();
            services.AddScoped<IMarketingPackageService, MarketingPackageService>();
            services.AddScoped<IProjectMarketingPackageService, ProjectMarketingPackageService>();

            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<ITaskOfferService, TaskOfferService>();
            services.AddScoped<ITaskAssignmentService, TaskAssignmentService>();
            services.AddScoped<ITaskEligibilityService, TaskEligibilityService>();
            services.AddScoped<ITaskReassignmentService, TaskReassignmentService>();
            services.AddScoped<ITaskDeadlineService, TaskDeadlineService>();
            return services;
        }
    }
}
