using System;

namespace AnyService.ComponentModel
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class AggregationAttribute : Attribute
    {
        public AggregationAttribute(string entityConfigRecordIdentifier)
        {
            EntityConfigRecordIdentifier = entityConfigRecordIdentifier;
        }

        public string EntityConfigRecordIdentifier { get; }
    }
}
