using System;

namespace AnyService.Services
{
    public sealed class ServiceResponseWrapper
    {
        public ServiceResponseWrapper(ServiceResponse serviceResponse)
        {
            ServiceResponse = serviceResponse;
        }
        public ServiceResponse ServiceResponse { get; }
        public Exception Exception { get; internal set; }
    }
}