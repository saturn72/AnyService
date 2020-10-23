namespace AnyService.Services.Internals
{
    public class AggregationData
    {
        public AggregationData(string externalName, EntityConfigRecord entityConfigRecord, bool isEnumerable)
        {
            ExternalName = externalName;
            EntityConfigRecord = entityConfigRecord;
            IsEnumerable = isEnumerable;
        }
        public string ExternalName { get; }
        public EntityConfigRecord EntityConfigRecord { get; }
        public bool IsEnumerable { get; }
    }
}
