using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ServiceResponseMappers
{
    public class DefaultServiceResponseMapper : IServiceResponseMapper
    {
        public IActionResult Map(ServiceResponse serviceResponse) => serviceResponse.ToActionResult();

        public IActionResult Map<TSource, TDestination>(ServiceResponse serviceResponse)
            where TSource : class
            where TDestination : class => serviceResponse.ToActionResult<TSource, TDestination>();
    }
}