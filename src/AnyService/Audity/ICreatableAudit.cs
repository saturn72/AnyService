namespace AnyService.Audity
{
    public interface ICreatableAudit
    {
        string CreatedOnUtc { get; set; }
        string CreatedByUserId { get; set; }
        string CreatedWorkContextJson { get; set; }
    }
}