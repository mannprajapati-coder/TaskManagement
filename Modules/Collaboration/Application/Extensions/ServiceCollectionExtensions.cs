using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Collaboration.Application.Services;
using Modules.Collaboration.Domain.IServices;
using Modules.Collaboration.Infrastructure.Context;

namespace Modules.Collaboration.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCollaborationModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=TaskManagement;Trusted_Connection=True;TrustServerCertificate=True;";

            services.AddDbContext<CollaborationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<ICollaborationService, CollaborationService>();

            return services;
        }
    }
}
