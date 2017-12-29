using Microsoft.AspNetCore.Hosting;
using NoDb;

namespace cloudscribe.Core.Storage.Git
{
    public class DefaultStoragePathOptionsResolverDecoratee : DefaultStoragePathOptionsResolver, IDefaultStoragePathOptionsResolverDecoratee
    {
        public DefaultStoragePathOptionsResolverDecoratee(IHostingEnvironment appEnv) : base(appEnv)
        { }
    }
}