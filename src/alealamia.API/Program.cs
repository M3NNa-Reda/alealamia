using alealamia.Infrastructure;
using alealamia.Infrastructure.Identity.Entities;
using alealamia.Infrastructure.Seeders;
using Microsoft.AspNetCore.Identity;
using alealamia.Application;

namespace EasyShop.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
            app.UseExceptionHandler("/error");
            //catches exceptions thrown in the following middlewares so it must come early
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();


            app.Run();
        }
    }
}