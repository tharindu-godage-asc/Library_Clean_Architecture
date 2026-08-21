using Library.Application.Identity;
using Library.Application.Interfaces;
using Library.Infrastructure.Data;
using Library.Infrastructure.Repositories;
using Library.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("LibraryDb");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Connection string 'LibraryDb' is not configured. Set it via the " +
                    "ConnectionStrings__LibraryDb environment variable, .NET user-secrets, " +
                    "or ConnectionStrings:LibraryDb in appsettings.");

            services.AddDbContext<LibraryDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Match the password rules RegisterRequestValidator already enforced
                // (min length 8, no complexity requirements) so nothing that passed
                // validation before starts failing here.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<LibraryDbContext>();
            // Not calling .AddDefaultTokenProviders() — that's only needed for password-reset/
            // email-confirmation/2FA token generation, none of which are implemented here.

            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<IBorrowingRepository, BorrowingRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITokenService, TokenService>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}