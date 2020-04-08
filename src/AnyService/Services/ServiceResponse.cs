namespace AnyService.Services
{
    public class ServiceResponse
    {
        public ServiceResponse()
        {
            Result = ServiceResult.NotSet;
        }
        public string Result { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public object ExceptionId { get; set; }
    }
}