using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoDb;

namespace cloudscribe.Core.Storage.Git
{
    public static class ServiceCollectionExtensions 
    {
        public static IServiceCollection AddCloudscribeCoreGitStorage(this IServiceCollection services)
        {
            services.TryAddScoped<IDefaultStoragePathOptionsResolverDecoratee, DefaultStoragePathOptionsResolverDecoratee>();
            services.TryAddScoped<IStoragePathOptionsResolver, StoragePathOptionsResolver>();

            services.AddCloudscribeCoreNoDbStorage();

            return services;
        }
    }
}
