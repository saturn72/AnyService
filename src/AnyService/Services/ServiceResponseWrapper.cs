using System;

namespace AnyService.Services
{
    public sealed class ServiceResponseWrapper<T>
    {
        public ServiceResponseWrapper(ServiceResponse<T> serviceResponse)
        {
            ServiceResponse = serviceResponse;
        }
        public ServiceResponse<T> ServiceResponse { get; }
        public Exception Exception { get; internal set; }
    }
}