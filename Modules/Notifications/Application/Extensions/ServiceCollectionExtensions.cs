using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Notifications.Application.Services;
using Modules.Notifications.Domain.IServices;
using Modules.Notifications.Infrastructure.Context;

namespace Modules.Notifications.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=TaskManagement;Trusted_Connection=True;TrustServerCertificate=True;";

            services.AddDbContext<NotificationsDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<INotificationService, NotificationService>();

            return services;
        }
    }
}
