using System;
using AnyService;
using AnyService.Events;
using AnyService.Services;
using AnyService.Utilities;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ServiceResponseWrapperExtensions
    {
        public static bool ValidateServiceResponseAndPublishException<T>(this ServiceResponseWrapper wrapper, string eventKey, object data)
        {
            if (wrapper.Exception == null)
                return wrapper.ServiceResponse.ValidateServiceResponse<T>();
         
            PublishException(wrapper.ServiceResponse, eventKey, data, wrapper.Exception);
            return false;
        }
        private static void PublishException(ServiceResponse serviceResponse, string eventKey, object data, Exception exception)
        {
            serviceResponse.ExceptionId = AppEngine.GetService<IIdGenerator>().GetNext();

            AppEngine.GetService<IEventBus>().Publish(eventKey, new DomainEventData
            {
                Data = new
                {
                    incomingObject = data,
                    exceptionId = serviceResponse.ExceptionId,
                    exception = exception
                },
                PerformedByUserId = AppEngine.GetService<WorkContext>().CurrentUserId
            });
        }
    }
}