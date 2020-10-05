namespace AnyService.SampleApp.Models
{
    public class CustomEntity : IDomainEntity
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }
}