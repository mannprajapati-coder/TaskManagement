using Microsoft.Extensions.DependencyInjection;
using Modules.UserManagement.Application.Services;
using Modules.UserManagement.Domain.IServices;

namespace Modules.UserManagement.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUserManagementModule(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
