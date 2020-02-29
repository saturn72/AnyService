using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ServiceResponseMappers
{
    public interface IServiceResponseMapper
    {
        IActionResult Map(ServiceResponse serviceResponse);
        IActionResult Map<TSource, TDestination>(ServiceResponse serviceResponse)
            where TSource : class
            where TDestination : class;
    }
}