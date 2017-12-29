using cloudscribe.SimpleContent.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoDb;

namespace cloudscribe.SimpleContent.Storage.Git
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGitStorageForSimpleContent(this IServiceCollection services)
        {
            services.TryAddScoped<IBasicCommandsDecoratee<Post>, BasicCommandsDecoratee<Post>>();
            services.TryAddScoped<IBasicCommandsDecoratee<Page>, BasicCommandsDecoratee<Page>>();
            services.TryAddScoped<IBasicCommands<Post>, BasicCommands<Post>>();
            services.TryAddScoped<IBasicCommands<Page>, BasicCommands<Page>>();

            services.AddNoDbStorageForSimpleContent();

            return services;
        }
    }
}
