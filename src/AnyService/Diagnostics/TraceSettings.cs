namespace AnyService.Diagnostics
{
    public class TraceSettings
    {
        /// <summary>
        /// Gets or sets value indicating if the tracer is disabled.
        /// </summary>
        public bool Disabled { get; set; }
        /// <summary>
        /// Gets or sets value speifies TracerName. default is "AnyService"
        /// </summary>
        public string TracerName { get; set; } = "AnyService";
    }
}
