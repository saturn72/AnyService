namespace AnyService
{
    public sealed class AnyServiceConfig
    {
        public AnyServiceConfig()
        {
            MaxMultipartBoundaryLength = 50;
            MaxValueCount = 25;
        }

        public int MaxMultipartBoundaryLength { get; set; }
        public int MaxValueCount { get; set; }
    }
}