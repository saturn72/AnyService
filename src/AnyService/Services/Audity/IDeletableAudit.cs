namespace AnyService.Audity
{
    public interface IDeletableAudit
    {
        bool Deleted { get; set; }
        string DeletedOnUtc { get; set; }
        string DeletedByUserId { get; set; }
    }
}