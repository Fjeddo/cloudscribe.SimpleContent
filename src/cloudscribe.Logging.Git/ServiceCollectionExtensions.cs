using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace cloudscribe.Logging.Git
{
    public static class ServiceCollectionExtensions 
    {
        public static IServiceCollection AddCloudscribeLoggingGitStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCloudscribeLoggingNoDbStorage(configuration);
            return services;
        }
    }
}
