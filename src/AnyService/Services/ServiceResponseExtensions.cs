using System;
using System.Collections.Generic;
using AnyService.Services;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ServiceResponseExtensions
    {
        private static readonly IDictionary<string, Func<ServiceResponse, IActionResult>> ConversionFuncs =
            new Dictionary<string, Func<ServiceResponse, IActionResult>>
            {
                {
                    ServiceResult.Accepted,
                    sr => sr.Data==null && !sr.Message.HasValue()?
                            new AcceptedResult() :
                            new AcceptedResult("", new{sr.Data,sr.Message})
                },
                {
                    ServiceResult.BadOrMissingData,
                    sr =>  sr.Data!=null || sr.Message.HasValue()?
                        new BadRequestObjectResult(new{ sr.Message, sr.Data}) :
                        new BadRequestResult() as IActionResult
                },
                {
                    ServiceResult.Error,
                    sr => {
                        if(sr.Data!=null || sr.Message.HasValue())
                        {
                            var res = new ObjectResult(new{ sr.Message, sr.Data});
                            res.StatusCode = StatusCodes.Status500InternalServerError;
                            return res;
                        }
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                },
                {
                    ServiceResult.NotFound,
                     sr =>  sr.Data!=null || sr.Message.HasValue()?
                        new NotFoundObjectResult(new{ sr.Message, sr.Data}) :
                        new NotFoundResult() as IActionResult
                },
                {
                    ServiceResult.NotSet,
                     sr =>  new StatusCodeResult(StatusCodes.Status500InternalServerError)
                },
                {
                    ServiceResult.Ok,
                    sr =>  sr.Data!=null || sr.Message.HasValue()?
                        new OkObjectResult(new{ sr.Message, sr.Data}) :
                        new OkResult() as IActionResult
                },
                {
                    ServiceResult.Unauthorized,
                    sr =>  sr.Data!=null || sr.Message.HasValue()?
                        new UnauthorizedObjectResult(new{ sr.Message, sr.Data}) :
                        new UnauthorizedResult() as IActionResult
                },
            };
        public static IActionResult ToActionResult(this ServiceResponse serviceResponse)
        {
            return ConversionFuncs[serviceResponse.Result](serviceResponse);
        }
    }
}