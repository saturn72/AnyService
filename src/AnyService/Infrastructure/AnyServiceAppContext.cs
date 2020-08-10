using System.Runtime.CompilerServices;

namespace AnyService.Infrastructure
{
    public class AnyServiceAppContext
    {
        private static IAppEngine _appEngine;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Init(IAppEngine appEngine) => _appEngine = appEngine;
        
        public static TService GetService<TService>() => _appEngine.GetService<TService>();

    }
}