using System.Collections.Generic;

namespace AnyService.Services.Internals
{
    internal class AggregationDataFactory
    {
        private IDictionary<string, EntityConfigRecord> _data;
        internal EntityConfigRecord GetByExternalName(string externalName)
        {
            _data.TryGetValue(externalName, out EntityConfigRecord ecr);
            return ecr;
        }
    }
}
