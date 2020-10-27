namespace AnyService.Services
{
    public class ServiceResponse<T> : ServiceResponse
    {
        public T Payload
        {
            get { return (T)PayloadObject; }
            set { PayloadObject = value; }
        }
    }
    public class ServiceResponse
    {
        public ServiceResponse()
        {
            Result = ServiceResult.NotSet;
        }
        public ServiceResponse(ServiceResponse source)
        {
            PayloadObject = source.PayloadObject;
            Result = source.Result;
            Message = source.Message;
            TraceId = source.TraceId;
        }
        internal object PayloadObject { get; set; }
        public string Result { get; set; }
        public string Message { get; set; }
        public object TraceId { get; set; }
        public object SpanId { get; set; }
    }
}