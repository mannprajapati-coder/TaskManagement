using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Tasks.Application.Services;
using Modules.Tasks.Domain.IServices;
using Modules.Tasks.Infrastructure.Context;

namespace Modules.Tasks.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTasksModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=TaskManagement;Trusted_Connection=True;TrustServerCertificate=True;";

            services.AddDbContext<TasksDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<ITasksService, TasksService>();
            services.AddScoped<ICalendarService, CalendarService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}
