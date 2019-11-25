namespace AnyService.EasyCaching
{
    public class EasyCachingConfig
    {
        public EasyCachingConfig()
        {
            DefaultCachingTimeInSeconds = 10 * 60;
            ProviderName = "default";
        }
        public uint DefaultCachingTimeInSeconds { get; set; }
        public string ProviderName { get; set; }
    }
}
