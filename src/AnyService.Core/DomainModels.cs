namespace AnyService
{
    public interface IDomainModelBase
    {
        string Id { get; set; }
    }
    public abstract class ChildModelBase : IDomainModelBase
    {
        public string Id { get; set; }
        public string ParentKey { get; set; }
        public string ParentId { get; set; }
    }
}