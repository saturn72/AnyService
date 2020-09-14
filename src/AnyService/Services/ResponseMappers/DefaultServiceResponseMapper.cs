using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AnyService.Services.ServiceResponseMappers
{
    public class DefaultServiceResponseMapper : IServiceResponseMapper
    {
        public IActionResult MapServiceResponse(ServiceResponse serviceResponse) => serviceResponse.ToActionResult();

        public IActionResult MapServiceResponse(Type source, Type destination, ServiceResponse serviceResponse) => serviceResponse.ToActionResult(source, destination);
    }
}