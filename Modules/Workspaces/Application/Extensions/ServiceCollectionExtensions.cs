using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Workspaces.Application.Services;
using Modules.Workspaces.Domain.IServices;
using Modules.Workspaces.Infrastructure.Context;

namespace Modules.Workspaces.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkspacesModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=TaskManagement;Trusted_Connection=True;TrustServerCertificate=True;";

            services.AddDbContext<WorkspacesDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IWorkspaceService, WorkspaceService>();

            return services;
        }
    }
}
