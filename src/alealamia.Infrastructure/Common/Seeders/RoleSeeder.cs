using alealamia.Domain.Constants;
using alealamia.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace alealamia.Infrastructure.Seeders
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {

            string[] roles =
            {
                AppRoles.Admin,
                AppRoles.Client,
                AppRoles.Freelancer
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(
                        new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        throw new Exception(
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            var adminSettings = configuration.GetSection("AdminSettings");

            var email = adminSettings["Email"];
            var password = adminSettings["Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new Exception("Admin email or password is missing.");
            }

            var admin = await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    FirstName = adminSettings["FirstName"] ?? "System",
                    LastName = adminSettings["LastName"] ?? "Admin",
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var createResult = await userManager.CreateAsync(
                    admin, password);

                if (!createResult.Succeeded)
                {
                    throw new Exception(
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
            {
                var roleResult = await userManager.AddToRoleAsync(
                    admin,
                    AppRoles.Admin);

                if (!roleResult.Succeeded)
                {
                    throw new Exception(
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}