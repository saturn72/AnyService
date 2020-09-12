using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AnyService.Services.ServiceResponseMappers
{
    public class DefaultServiceResponseMapper : IServiceResponseMapper
    {
        public IActionResult Map(ServiceResponse serviceResponse) => serviceResponse.ToActionResult();

        public IActionResult Map(Type source, Type destination, ServiceResponse serviceResponse) => serviceResponse.ToActionResult(source, destination);
    }
}