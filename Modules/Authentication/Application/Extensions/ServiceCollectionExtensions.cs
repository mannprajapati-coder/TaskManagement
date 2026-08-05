using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Authentication.Application.Services;
using Modules.Authentication.Domain.IServices;
using Modules.Authentication.Infrastructure.Context;
using Modules.UserManagement.Infrastructure.Context;

namespace Modules.Authentication.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthenticationModule(this IServiceCollection services, IConfiguration configuration)
        {
            var authConnectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Server=(localdb)\\mssqllocaldb;Database=TaskPlatformDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            services.AddDbContext<UserManagementDbContext>(options =>
                options.UseSqlServer(authConnectionString));

            services.AddDbContext<AuthenticationDbContext>(options =>
                options.UseSqlServer(authConnectionString));

            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IEmailSender, DevEmailSender>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
