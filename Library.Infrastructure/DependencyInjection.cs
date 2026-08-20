using Library.Application.Interfaces;
using Library.Infrastructure.Data;
using Library.Infrastructure.Repositories;
using Library.Infrastructure.Security;
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

            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<IBorrowingRepository, BorrowingRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            return services;
        }
    }
}