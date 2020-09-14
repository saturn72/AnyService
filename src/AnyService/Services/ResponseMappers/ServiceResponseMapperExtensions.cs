using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ServiceResponseMappers
{
    public static class ServiceResponseMapperExtensions
    {
        public static IActionResult MapServiceResponse<TSource, TDestination>(this IServiceResponseMapper mapper, ServiceResponse serviceResponse)
            where TSource : class
            where TDestination : class
        {
            return mapper.MapServiceResponse(typeof(TSource), typeof(TDestination), serviceResponse);
        }
    }
}