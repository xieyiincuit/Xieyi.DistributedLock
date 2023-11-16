using System.Runtime.CompilerServices;

//需要扩展的类型需要在此添加对应的程序集友元标识
[assembly: InternalsVisibleTo("Xieyi.DistributedLock")]
[assembly: InternalsVisibleTo("Xieyi.DistributedLock.Exception")]
[assembly: InternalsVisibleTo("Xieyi.DistributedLock.Connection")]
[assembly: InternalsVisibleTo("Xieyi.DistributedLock.Interfaces")]
[assembly: InternalsVisibleTo("Xieyi.DistributedLock.LockLimit")]
[assembly: InternalsVisibleTo("Xieyi.DistributedLock.Renew")]
[assembly: InternalsVisibleTo("Xieyi.DistributedLock.Test")]
namespace Xieyi.DistributedLock
{
    
    internal class AssemblyInternalsVisibleControl { }
}
