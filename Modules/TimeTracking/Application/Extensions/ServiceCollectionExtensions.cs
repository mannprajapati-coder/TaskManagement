using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.TimeTracking.Application.Services;
using Modules.TimeTracking.Domain.IServices;
using Modules.TimeTracking.Infrastructure.Context;

namespace Modules.TimeTracking.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTimeTrackingModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=TaskManagement;Trusted_Connection=True;TrustServerCertificate=True;";

            services.AddDbContext<TimeTrackingDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<ITimeTrackingService, TimeTrackingService>();

            return services;
        }
    }
}
