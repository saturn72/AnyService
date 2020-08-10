using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.Infrastructure
{
    public class DefaultAppEngine : IAppEngine
    {
        #region Fields
        private readonly IServiceProvider _serviceProvier;
        #endregion
        #region
        public DefaultAppEngine(IServiceProvider serviceProvider)
        {
            _serviceProvier = serviceProvider;
        }
        #endregion
        public TService GetService<TService>() => _serviceProvier.GetService<TService>();
    }
}