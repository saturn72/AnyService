namespace AnyService
{
    public interface IEntity : IDbRecord<string>
    { }
    public interface IDbRecord<TId>
    {
        TId Id { get; set; }
    }

    public abstract class ChildModelBase : IEntity
    {
        public string Id { get; set; }
        public string ParentKey { get; set; }
        public string ParentId { get; set; }
    }
}