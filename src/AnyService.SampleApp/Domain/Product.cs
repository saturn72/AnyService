namespace AnyService.SampleApp.Domain
{
    public class Product : IDomainEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
