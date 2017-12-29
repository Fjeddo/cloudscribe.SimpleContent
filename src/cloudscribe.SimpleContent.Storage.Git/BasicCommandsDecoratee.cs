using Microsoft.Extensions.Logging;
using NoDb;

namespace cloudscribe.SimpleContent.Storage.Git
{
    public class BasicCommandsDecoratee<T> : global::NoDb.BasicCommands<T>, IBasicCommandsDecoratee<T> where T : class
    {
        public BasicCommandsDecoratee(ILogger<global::NoDb.BasicCommands<T>> logger, IStoragePathResolver<T> pathResolver, IStringSerializer<T> serializer) : base(logger, pathResolver, serializer)
        { }
    }
}