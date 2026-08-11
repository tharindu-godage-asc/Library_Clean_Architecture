using Library.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<BookService>();
            services.AddScoped<MemberService>();
            services.AddScoped<BorrowingService>();

            return services;
        }
    }
}
