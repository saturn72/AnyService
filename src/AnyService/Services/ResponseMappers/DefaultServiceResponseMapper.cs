using AnyService.Services.ResponseMappers;
using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services
{
    public class DefaultServiceResponseMapper : IServiceResponseMapper
    {
        public IActionResult Map(ServiceResponse serviceResponse) => serviceResponse.ToActionResult();
    }
}