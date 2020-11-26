using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ServiceResponseMappers
{
    public static class ServiceResponseMapperExtensions
    {
        public static IActionResult MapServiceResponse<TDestination>(this IServiceResponseMapper serviceResponseMapper, ServiceResponse serviceResponse)
            where TDestination : class
        {
            return serviceResponseMapper.MapServiceResponse(typeof(TDestination), serviceResponse);
        }
    }
}