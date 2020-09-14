using System;
using AnyService;
using AnyService.Events;
using AnyService.Services;
using AnyService.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ServiceResponseWrapperExtensions
    {
        private static IServiceProvider _serviceProvider;
        public static void Init(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
        public static bool ValidateServiceResponseAndPublishException<T>(this ServiceResponseWrapper<T> wrapper, string eventKey, object data)
        {
            if (!PublishExceptionIfExists<T>(wrapper, eventKey, data))
                return wrapper.ServiceResponse.ValidateServiceResponse<T>();

            return false;
        }
        public static bool PublishExceptionIfExists<T>(this ServiceResponseWrapper<T> wrapper, string eventKey, object data)
        {
            if (wrapper.Exception == null)
                return false;
            PublishException(wrapper.ServiceResponse, eventKey, data, wrapper.Exception);
            return true;
        }
        private static void PublishException(ServiceResponse serviceResponse, string eventKey, object data, Exception exception)
        {
            serviceResponse.ExceptionId = _serviceProvider.GetService<IIdGenerator>().GetNext();

            _serviceProvider.GetService<IEventBus>().Publish(eventKey, new DomainEventData
            {
                Data = new
                {
                    incomingObject = data,
                    exceptionId = serviceResponse.ExceptionId,
                    exception = exception
                },
                PerformedByUserId = _serviceProvider.GetService<WorkContext>().CurrentUserId
            });
        }
    }
}