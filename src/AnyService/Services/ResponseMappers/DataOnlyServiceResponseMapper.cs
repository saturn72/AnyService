using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnyService.Services.ServiceResponseMappers
{
    public class DataOnlyServiceResponseMapper : IServiceResponseMapper
    {
        #region fields
        public static readonly IDictionary<string, Func<ServiceResponse, IActionResult>> ConversionFuncs =
        new Dictionary<string, Func<ServiceResponse, IActionResult>>
            {
                {
                    ServiceResult.Accepted,
                    sr => sr.PayloadObject==null && !sr.Message.HasValue()?
                            new AcceptedResult() :
                            new AcceptedResult("", sr.PayloadObject)
                },
                {
                    ServiceResult.BadOrMissingData,
                    sr =>  sr.PayloadObject!=null || sr.Message.HasValue()?
                        new BadRequestObjectResult(sr.PayloadObject) :
                        new BadRequestResult() as IActionResult
                },
                {
                    ServiceResult.Error,
                    sr => {
                        if(sr.PayloadObject!=null || sr.Message.HasValue())
                        {
                            return new ObjectResult( sr.PayloadObject) { StatusCode = StatusCodes.Status500InternalServerError
};
                        }
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                },
                {
                    ServiceResult.NotFound,
                     sr =>  sr.PayloadObject!=null || sr.Message.HasValue()?
                        new NotFoundObjectResult( sr.PayloadObject) :
                        new NotFoundResult() as IActionResult
                },
                {
                    ServiceResult.NotSet,
                     sr =>  new StatusCodeResult(StatusCodes.Status500InternalServerError)
                },
                {
                    ServiceResult.Ok,
                    sr =>  sr.PayloadObject!=null || sr.Message.HasValue()?
                        new OkObjectResult( sr.PayloadObject) :
                        new OkResult() as IActionResult
                },
                {
                    ServiceResult.Unauthorized,
                    sr =>  sr.PayloadObject!=null || sr.Message.HasValue()?
                        new UnauthorizedObjectResult( sr.PayloadObject) :
                        new UnauthorizedResult() as IActionResult
                },
            };
        private readonly AnyServiceConfig _config;
        #endregion
        #region ctor
        public DataOnlyServiceResponseMapper(AnyServiceConfig config)
        {
            _config = config;
        }
        #endregion
        public IActionResult MapServiceResponse(ServiceResponse serviceResponse) => ConversionFuncs[serviceResponse.Result](serviceResponse);
        public IActionResult MapServiceResponse(Type source, Type destination, ServiceResponse serviceResponse)
        {
            if (serviceResponse.PayloadObject != null)
            {
                var c = new ServiceResponse
                {
                    ExceptionId = serviceResponse.ExceptionId,
                    Message = serviceResponse.Message,
                    PayloadObject = serviceResponse.PayloadObject.Map(destination, _config.MapperName),
                    Result = serviceResponse.Result
                };
                return MapServiceResponse(c);
            }
            return MapServiceResponse(serviceResponse);
        }
    }
}