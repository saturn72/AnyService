using System;

namespace AnyService.Utilities
{
    public sealed class GuidIdGenerator : IIdGenerator
    {
        public object GetNext() => Guid.NewGuid();
    }
}