using System;
using System.Threading.Tasks;

namespace AZ.TcpNet.Base
{
    internal static class AsyncHelper
    {

        public static Task Run(Action action)
        {
#if NET40
            return TaskEx.Run( action);
#else
            
              return Task.Run(action);
#endif
        }
    }
}
