using System.ComponentModel;

namespace AnyService.EasyCaching
{
    public class EasyCachingConfig
    {
        [DefaultValue(600)]
        public uint DefaultCachingTimeInSeconds { get; set; } = 600;
    }
}
