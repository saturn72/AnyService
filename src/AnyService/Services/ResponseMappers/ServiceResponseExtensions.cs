using System;
using System.Collections.Generic;
using AnyService;
using AnyService.Services;
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
                            return new ObjectResult(new{ sr.Message, sr.Data}) { StatusCode = StatusCodes.Status500InternalServerError };
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
        public static IActionResult ToActionResult<TSource, TDestination>(this ServiceResponse serviceResponse)
          where TSource : class
          where TDestination : class
        {
            if (serviceResponse.Data != null)
            {
                if (!typeof(TSource).IsAssignableFrom(serviceResponse.Data.GetType()))
                    throw new InvalidOperationException($"Cannot map from {serviceResponse.Data.GetType()} to {typeof(TSource)}");
                serviceResponse.Data = serviceResponse.Data.Map<TDestination>();
            }

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
           (serviceResponse?.Result == ServiceResult.Ok && serviceResponse.Data is T) || serviceResponse?.Result == ServiceResult.Accepted;
    }
}