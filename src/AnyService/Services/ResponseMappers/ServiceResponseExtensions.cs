using System;
using System.Collections.Generic;
using AnyService;
using AnyService.Services;
using AnyService.Utilities;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ServiceResponseExtensions
    {
        public static readonly IDictionary<string, Func<ServiceResponse, IActionResult>> ConversionFuncs =
            new Dictionary<string, Func<ServiceResponse, IActionResult>>
            {
                {
                    ServiceResult.Accepted,
                    sr => sr.PayloadObject==null && !sr.Message.HasValue()?
                            new AcceptedResult() :
                            new AcceptedResult("", new{sr.PayloadObject,sr.Message})
                },
                {
                    ServiceResult.BadOrMissingData,
                    sr =>  sr.PayloadObject!=null || sr.Message.HasValue()?
                        new BadRequestObjectResult(new{ message = sr.Message, data = sr.PayloadObject}) :
                        new BadRequestResult() as IActionResult
                },
                {
                    ServiceResult.Error,
                    sr => {
                        if(sr.PayloadObject!=null || sr.Message.HasValue())
                        {
                            return new ObjectResult(new{ message = sr.Message, data = sr.PayloadObject}) { StatusCode = StatusCodes.Status500InternalServerError };
                        }
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                },
                {
                    ServiceResult.NotFound,
                     sr =>  sr.PayloadObject!=null || sr.Message.HasValue()?
                        new NotFoundObjectResult(new{ message = sr.Message, data = sr.PayloadObject}) :
                        new NotFoundResult() as IActionResult
                },
                {
                    ServiceResult.NotSet,
                     sr =>  new StatusCodeResult(StatusCodes.Status500InternalServerError)
                },
                {
                    ServiceResult.Ok,
                    sr =>  sr.PayloadObject!=null || sr.Message.HasValue()?
                        new OkObjectResult(new{ message = sr.Message, data = sr.PayloadObject}) :
                        new OkResult() as IActionResult
                },
                {
                    ServiceResult.Unauthorized,
                    sr =>  sr.PayloadObject!=null || sr.Message.HasValue()?
                        new UnauthorizedObjectResult(new{ message = sr.Message, data = sr.PayloadObject}) :
                        new UnauthorizedResult() as IActionResult
                },
            };
        public static IActionResult ToActionResult<TDestination>(
            this ServiceResponse serviceResponse,
            string mapperName)
          where TDestination : class => ToActionResult(serviceResponse, typeof(TDestination), mapperName);

        public static IActionResult ToActionResult(this ServiceResponse serviceResponse, Type destination, string mapperName)
        {
            if (serviceResponse.PayloadObject != null)
                serviceResponse.PayloadObject = serviceResponse.PayloadObject.Map(destination, mapperName);

            return ToActionResult(serviceResponse);
        }
        public static IActionResult ToActionResult(this ServiceResponse serviceResponse) => ConversionFuncs[serviceResponse.Result](serviceResponse);

        public static readonly IDictionary<string, int> ToStatusCodes = new Dictionary<string, int>
        {
            {ServiceResult. Accepted , StatusCodes.Status202Accepted },
            {ServiceResult.BadOrMissingData , StatusCodes.Status400BadRequest },
            {ServiceResult.Error, StatusCodes.Status500InternalServerError },
            { ServiceResult.NotFound , StatusCodes.Status404NotFound },
            {ServiceResult.NotSet , StatusCodes.Status403Forbidden },
            {ServiceResult.Ok , StatusCodes.Status200OK},
            {ServiceResult.Unauthorized , StatusCodes.Status401Unauthorized },
        };
        public static int ToHttpStatusCode(this ServiceResponse serviceResponse)
        {
            return ToStatusCodes.TryGetValue(serviceResponse.Result, out int value) ?
                value : StatusCodes.Status500InternalServerError;
        }
        public static bool ValidateServiceResponse<T>(this ServiceResponse serviceResponse) =>
           (serviceResponse?.Result == ServiceResult.Ok && serviceResponse.PayloadObject is T) || serviceResponse?.Result == ServiceResult.Accepted;
    }
}