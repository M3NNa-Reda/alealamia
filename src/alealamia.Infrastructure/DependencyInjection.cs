using alealamia.Infrastructure.Common.Options;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Infrastructure.Authentication;
using alealamia.Infrastructure.Identity;
using alealamia.Infrastructure.Identity.Entities;
using alealamia.Infrastructure.Persistence.Context;
using alealamia.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace alealamia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
             options.UseSqlServer(configuration.GetConnectionString("ConnectionString")));
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
             ?? throw new InvalidOperationException("Jwt configuration section is missing or invalid.");

        services.AddSingleton(jwtOptions);

        services.AddScoped<IJwtService, JwtService>();

        // add authentication schema + how to validate token
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
            };
        });
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();

        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        services.AddScoped<ITaskSubmissionRepository, TaskSubmissionRepository>();
        services.AddScoped<ITaskReviewRepository, TaskReviewRepository>();
        services.AddScoped<ITaskRevisionRepository, TaskRevisionRepository>();
        services.AddScoped<ISubmissionFileRepository, SubmissionFileRepository>();
        services.AddScoped<IFreelancerLedgerEntryRepository, FreelancerLedgerEntryRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ISupportMessageRepository, SupportMessageRepository>();
        services.AddScoped<ISupportMessageFileRepository, SupportMessageFileRepository>();

        services.AddScoped<IFreelancerProfileRepository, FreelancerProfileRepository>();
        services.AddScoped<IFreelancerBlockRepository, FreelancerBlockRepository>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}