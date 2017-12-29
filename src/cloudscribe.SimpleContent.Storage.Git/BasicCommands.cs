using System.Threading;
using System.Threading.Tasks;
using NoDb;

namespace cloudscribe.SimpleContent.Storage.Git
{
    public class BasicCommands<T> : IBasicCommands<T> where T : class
    {
        private readonly IBasicCommandsDecoratee<T> _decoratee;

        public BasicCommands(IBasicCommandsDecoratee<T> decoratee)
        {
            _decoratee = decoratee;
        }

        public Task CreateAsync(string projectId, string key, T obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return _decoratee.CreateAsync(projectId, key, obj, cancellationToken);
        }

        public Task UpdateAsync(string projectId, string key, T obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return _decoratee.UpdateAsync(projectId, key, obj, cancellationToken);
        }

        public Task DeleteAsync(string projectId, string key, CancellationToken cancellationToken = new CancellationToken())
        {
            return _decoratee.DeleteAsync(projectId, key, cancellationToken);
        }

        public void Dispose()
        { }
    }
}