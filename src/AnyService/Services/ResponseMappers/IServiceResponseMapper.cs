using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ResponseMappers
{
    public interface IServiceResponseMapper
    {
        IActionResult Map(ServiceResponse serviceResponse);
    }
}