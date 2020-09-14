using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ServiceResponseMappers
{
    public class DataOnlyServiceResponseMapper : IServiceResponseMapper
    {
        public static readonly IDictionary<string, Func<ServiceResponse, IActionResult>> ConversionFuncs =
            new Dictionary<string, Func<ServiceResponse, IActionResult>>
            {
                {
                    ServiceResult.Accepted,
                    sr => sr.Data==null && !sr.Message.HasValue()?
                            new AcceptedResult() :
                            new AcceptedResult("", sr.Data)
                },
                {
                    ServiceResult.BadOrMissingData,
                    sr =>  sr.Data!=null || sr.Message.HasValue()?
                        new BadRequestObjectResult(sr.Data) :
                        new BadRequestResult() as IActionResult
                },
                {
                    ServiceResult.Error,
                    sr => {
                        if(sr.Data!=null || sr.Message.HasValue())
                        {
                            return new ObjectResult( sr.Data) { StatusCode = StatusCodes.Status500InternalServerError
};
                        }
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                },
                {
                    ServiceResult.NotFound,
                     sr =>  sr.Data!=null || sr.Message.HasValue()?
                        new NotFoundObjectResult( sr.Data) :
                        new NotFoundResult() as IActionResult
                },
                {
                    ServiceResult.NotSet,
                     sr =>  new StatusCodeResult(StatusCodes.Status500InternalServerError)
                },
                {
                    ServiceResult.Ok,
                    sr =>  sr.Data!=null || sr.Message.HasValue()?
                        new OkObjectResult( sr.Data) :
                        new OkResult() as IActionResult
                },
                {
                    ServiceResult.Unauthorized,
                    sr =>  sr.Data!=null || sr.Message.HasValue()?
                        new UnauthorizedObjectResult( sr.Data) :
                        new UnauthorizedResult() as IActionResult
                },
            };
        public IActionResult MapServiceResponse(ServiceResponse serviceResponse) => ConversionFuncs[serviceResponse.Result](serviceResponse);
        public IActionResult MapServiceResponse(Type source, Type destination, ServiceResponse serviceResponse)
        {
            if (serviceResponse.Data != null)
            {
                if (!source.IsAssignableFrom(serviceResponse.Data.GetType()))
                    throw new InvalidOperationException($"Cannot map from {serviceResponse.Data.GetType()} to {source}");
                serviceResponse.Data = serviceResponse.Data.Map(destination);
            }
            return MapServiceResponse(serviceResponse);
        }
    }
}