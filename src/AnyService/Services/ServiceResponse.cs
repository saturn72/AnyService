namespace AnyService.Services
{
    public class ServiceResponse<T>
    {
        internal object PayloadObject { get; private set; }
        public ServiceResponse()
        {
            Result = ServiceResult.NotSet;
        }
        public string Result { get; set; }
        public string Message { get; set; }
        public T Payload
        {
            get { return (T)PayloadObject; }
            set { PayloadObject = value; }
        }
        public object ExceptionId { get; set; }
    }
}