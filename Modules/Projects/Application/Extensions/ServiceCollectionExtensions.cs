using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Projects.Application.Services;
using Modules.Projects.Domain.IServices;
using Modules.Projects.Infrastructure.Context;

namespace Modules.Projects.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProjectsModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=TaskManagement;Trusted_Connection=True;TrustServerCertificate=True;";

            services.AddDbContext<ProjectsDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IProjectsService, ProjectsService>();

            return services;
        }
    }
}
