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
            this.PayloadObject = source.PayloadObject;
            this.Result = source.Result;
            this.Message = source.Message;
            this.ExceptionId = source.ExceptionId;
        }
        internal object PayloadObject { get; set; }
        public string Result { get; set; }
        public string Message { get; set; }
        public object ExceptionId { get; set; }
    }
}