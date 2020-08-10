using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService
{
    public class AppEngine
    {
        #region Fields
        private static IServiceProvider _serviceProvier;
        #endregion
        public static void Init(IServiceProvider serviceProvider)
        {
            _serviceProvier = serviceProvider;
        }
        public static TService GetService<TService>() => _serviceProvier.GetService<TService>();
    }
}