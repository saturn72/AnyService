namespace AnyService.Core.Security
{
    public class EntityPermission : ChildModelBase
    {
        public bool Excluded { get; set; }
        public string PermissionKey { get; set; }
        public string EntityKey { get; set; }
        public string EntityId { get; set; }
    }
}
