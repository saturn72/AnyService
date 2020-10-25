using Microsoft.AspNetCore.Mvc;
using System;

namespace AnyService.Services.ServiceResponseMappers
{
    public class DefaultServiceResponseMapper : IServiceResponseMapper
    {
        #region fields
        private readonly AnyServiceConfig _config;
        #endregion 
        #region ctor
        public DefaultServiceResponseMapper(AnyServiceConfig config)
        {
            _config = config;
        }
        #endregion
        public IActionResult MapServiceResponse(ServiceResponse serviceResponse) => serviceResponse.ToActionResult();

        public IActionResult MapServiceResponse(Type destination, ServiceResponse serviceResponse) => serviceResponse.ToActionResult(destination, _config.MapperName);
    }
}