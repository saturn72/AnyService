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
            if (!PublishExceptionIfExists(wrapper, eventKey, data))
                return wrapper.ServiceResponse.ValidateServiceResponse<T>();

            return false;
        }
        public static bool PublishExceptionIfExists(this ServiceResponseWrapper wrapper, string eventKey, object data)
        {
            if (wrapper.Exception == null)
                return false;
            PublishException(wrapper.ServiceResponse, eventKey, data, wrapper.Exception);
            return true;
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