namespace AnyService
{
    public interface IEntity : IDbModel<string> { }
    public interface IDbModel<TId>
    {
        TId Id { get; set; }
    }
    public sealed class EntityMappingRecord : IEntity
    {
        public string Id { get; set; }
        public string ParentEntityName { get; set; }
        public string ParentId { get; set; }
        public string ChildEntityName { get; set; }
        public string ChildId { get; set; }
    }

    public abstract class ChildModelBase : IEntity
    {
        public string Id { get; set; }
        public string ParentKey { get; set; }
        public string ParentId { get; set; }
    }
}