using System;
using AnyService;
using AnyService.Events;
using AnyService.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    public static class ServiceResponseWrapperExtensions
    {
        private static IServiceProvider _serviceProvider;
        public static void Init(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
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
            using var scope = _serviceProvider.CreateScope();
            var wc = scope.ServiceProvider.GetService<WorkContext>();
            serviceResponse.TraceId = wc.TraceId;
            serviceResponse.SpanId = wc.SpanId;

            var eb = scope.ServiceProvider.GetService<IDomainEventBus>();
            eb.PublishException(eventKey, exception, data, wc);
        }
    }
}