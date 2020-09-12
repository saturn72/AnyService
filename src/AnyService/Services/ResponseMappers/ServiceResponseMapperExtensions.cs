using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ServiceResponseMappers
{
    public static class ServiceResponseMapperExtensions
    {
        public static IActionResult Map<TSource, TDestination>(this IServiceResponseMapper mapper, ServiceResponse serviceResponse)
            where TSource : class
            where TDestination : class
        {
            return mapper.Map(typeof(TSource), typeof(TDestination), serviceResponse);
        }
    }
}