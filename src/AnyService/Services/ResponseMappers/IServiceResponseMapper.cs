using Microsoft.AspNetCore.Mvc;
using System;

namespace AnyService.Services.ServiceResponseMappers
{
    public interface IServiceResponseMapper
    {
        IActionResult MapServiceResponse(ServiceResponse serviceResponse);
        IActionResult MapServiceResponse(Type source, Type destination, ServiceResponse serviceResponse);
    }
}