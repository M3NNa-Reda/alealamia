using alealamia.Infrastructure.Identity.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EasyShop.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString")));


            //builder.Services
            //    .AddIdentityCore<ApplicationUser>(options =>
            //    {
            //        options.User.RequireUniqueEmail = true;

            //        options.Password.RequiredLength = 8;
            //        options.Password.RequireDigit = true;
            //        options.Password.RequireUppercase = true;
            //        options.Password.RequireLowercase = true;
            //        options.Password.RequireNonAlphanumeric = false;

            //        options.Lockout.AllowedForNewUsers = true;
            //        options.Lockout.MaxFailedAccessAttempts = 5;
            //        options.Lockout.DefaultLockoutTimeSpan =
            //            TimeSpan.FromMinutes(5);
            //    })
            //    .AddRoles<IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders()
            //    .AddSignInManager();

            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();
            //builder.Services.AddClientServices();

            var app = builder.Build();
            app.UseExceptionHandler("/error");
            //catches exceptions thrown in the following middlewares so it must come early
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();


            app.Run();
        }
    }
}