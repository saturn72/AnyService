namespace AnyService.SampleApp.Domain
{
    public class Product : IDomainObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
