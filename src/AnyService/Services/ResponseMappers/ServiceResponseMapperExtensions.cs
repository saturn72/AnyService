using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ServiceResponseMappers
{
    public static class ServiceResponseMapperExtensions
    {
        public static IActionResult MapServiceResponse<TDestination>(this IServiceResponseMapper mapper, ServiceResponse serviceResponse)
            where TDestination : class
        {
            return mapper.MapServiceResponse(typeof(TDestination), serviceResponse);
        }
    }
}