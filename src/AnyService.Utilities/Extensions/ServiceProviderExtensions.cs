namespace System
{
    public static class ServiceProviderExtensions
    {
        public static TService GetGenericService<TService>(this IServiceProvider serviceProvider, Type genericService, params Type[] types) =>
            (TService)GetGenericService(serviceProvider, genericService, types);
        public static object GetGenericService(this IServiceProvider serviceProvider, Type genericService, params Type[] types)
        {
            var serviceType = genericService.MakeGenericType(types);
            return serviceProvider.GetService(serviceType);
        }
    }
}
