using Library.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Library.Infrastructure.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");

            var email = configuration["AdminSeed:Email"];
            var password = configuration["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("AdminSeed:Email/Password not configured — skipping admin seeding.");
                return;
            }

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            if (!await roleManager.RoleExistsAsync(Roles.Admin))
                await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));

            if (await userManager.FindByEmailAsync(email) is not null)
            {
                logger.LogInformation("Admin seed skipped: {Email} already exists.", email);
                return;
            }

            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                MemberId = Guid.Empty
            };

            var result = await userManager.CreateAsync(admin, password);

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Admin seed failed for {Email}: {Errors}",
                    email,
                    string.Join(", ", result.Errors.Select(e => e.Code)));
                return;
            }

            await userManager.AddToRoleAsync(admin, Roles.Admin);
            logger.LogInformation("Seeded Admin account {Email}.", email);
        }
    }
}
