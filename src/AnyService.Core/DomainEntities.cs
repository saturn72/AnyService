namespace AnyService
{
    public interface IDomainEntity
    {
        string Id { get; set; }
    }

    public abstract class ChildModelBase : IDomainEntity
    {
        public string Id { get; set; }
        public string ParentKey { get; set; }
        public string ParentId { get; set; }
    }
}