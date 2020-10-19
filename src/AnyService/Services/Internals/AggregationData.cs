namespace AnyService.Services.Internals
{
    internal class AggregationData
    {
        public AggregationData(string name, EntityConfigRecord entityConfigRecord, bool isEnumerable)
        {
            Name = name;
            EntityConfigRecord = entityConfigRecord;
            IsEnumerable = isEnumerable;
        }
        public string Name { get; }
        public EntityConfigRecord EntityConfigRecord { get; }
        public bool IsEnumerable { get; }
    }
}
