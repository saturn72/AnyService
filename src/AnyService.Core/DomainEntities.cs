namespace AnyService
{
    public interface IDomainEntity
    {
        string Id { get; set; }
    }
    public sealed class EntityMapping : IDomainEntity
    {
        public string Id { get; set; }
        public string ParentEntityName { get; set; }
        public string ParentId { get; set; }
        public string ChildEntityName { get; set; }
        public string ChildId { get; set; }
    }

    public abstract class ChildModelBase : IDomainEntity
    {
        public string Id { get; set; }
        public string ParentKey { get; set; }
        public string ParentId { get; set; }
    }
}