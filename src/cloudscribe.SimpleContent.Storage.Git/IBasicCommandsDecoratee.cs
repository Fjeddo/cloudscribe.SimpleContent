using NoDb;

namespace cloudscribe.SimpleContent.Storage.Git
{
    public interface IBasicCommandsDecoratee<T> : IBasicCommands<T> where T : class { }
}