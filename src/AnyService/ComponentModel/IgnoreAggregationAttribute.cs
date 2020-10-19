using System;

namespace AnyService.ComponentModel
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class IgnoreAggregationAttribute : Attribute
    {
    }
}
