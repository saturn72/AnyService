namespace AnyService
{
    public interface IEntity : IDbModel<string>
    { }
    public interface IDbModel<TId>
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