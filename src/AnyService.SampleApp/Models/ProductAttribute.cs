namespace AnyService.SampleApp.Models
{
    public class ProductAttribute : IDomainObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
