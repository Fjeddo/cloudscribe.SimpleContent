using System.Threading.Tasks;
using NoDb;

namespace cloudscribe.Core.Storage.Git
{
    public class StoragePathOptionsResolver : IStoragePathOptionsResolver
    {
        private readonly IDefaultStoragePathOptionsResolverDecoratee _decoratee;

        public StoragePathOptionsResolver(IDefaultStoragePathOptionsResolverDecoratee decoratee)
        {
            _decoratee = decoratee;
        }

        public Task<StoragePathOptions> Resolve(string projectId)
        {
            return Task.Run(() =>
            {
                var options = _decoratee.Resolve(projectId).Result;
                options.BaseFolderVPath = "/git_storage";

                return options;
            });
        }
    }
}