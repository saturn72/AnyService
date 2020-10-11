using System;

namespace AnyService.Services
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class AggregatedAttribute : Attribute
    {
        public AggregatedAttribute(string entityName, string externalName = null)
        {
            EntityName = entityName;
            ExternalName = externalName ?? entityName;
        }
        /// <summary>
        /// Gets the Entity name to be used to get metadata for the aggregated child
        /// </summary>
        public string EntityName { get; }
        /// <summary>
        /// Gets Wntity external name. This is the name you should use with external API
        /// </summary>
        public string ExternalName { get; }
    }
}
