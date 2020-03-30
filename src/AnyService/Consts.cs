namespace AnyService
{
    public class Consts
    {
        public const string ReservedPrefix = "__";
        public const string MultipartSuffix = ReservedPrefix + "multipart";
        public const string StreamSuffix = ReservedPrefix + "stream";
        public const string PublicSuffix = ReservedPrefix + "public";
        public const string ControllerAuthzPolicy = "controller-policy";
    }
}