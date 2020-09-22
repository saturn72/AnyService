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
        internal object PayloadObject { get; set; }
        public ServiceResponse()
        {
            Result = ServiceResult.NotSet;
        }
        public string Result { get; set; }
        public string Message { get; set; }
        public object ExceptionId { get; set; }
    }
}