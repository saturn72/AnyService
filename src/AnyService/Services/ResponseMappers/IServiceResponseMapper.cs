using Microsoft.AspNetCore.Mvc;
using System;

namespace AnyService.Services.ServiceResponseMappers
{
    public interface IServiceResponseMapper
    {
        IActionResult Map(ServiceResponse serviceResponse);
        IActionResult Map(Type source, Type destination, ServiceResponse serviceResponse);
    }
}