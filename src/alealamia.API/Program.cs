using alealamia.API.Background;
using alealamia.API.Middleware;
using alealamia.API.Security;
using alealamia.Application;
using alealamia.Infrastructure;
using alealamia.Infrastructure.Identity.Entities;
using alealamia.Infrastructure.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;

namespace EasyShop.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            builder.Services.AddApiRateLimiting();
            builder.Services.AddFrontendCors();
            builder.Services.AddOptions<DeadlineMonitorOptions>().BindConfiguration(DeadlineMonitorOptions.SectionName);
            builder.Services.AddHostedService<DeadlineMonitor>();
            builder.Services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
            });
            builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ---- Swagger services ----
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AlAlamia API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = ""
                });

                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });
            // ---------------------------
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var configuration = services.GetRequiredService<IConfiguration>();

                await RoleSeeder.SeedAsync(
                    roleManager,
                    userManager,
                    configuration);
            }
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseExceptionHandler();
            //catches exceptions thrown in the following middlewares so it must come early
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors(CorsExtensions.FrontendPolicy);
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();


            app.Run();
        }
    }
}