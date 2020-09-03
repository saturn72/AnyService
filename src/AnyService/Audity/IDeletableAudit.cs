namespace AnyService.Audity
{
    public interface IDeletableAudit
    {
        string DeletedOnUtc { get; set; }
        string DeletedByUserId { get; set; }
    }
}