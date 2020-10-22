namespace AnyService.SampleApp.Models
{
    public class ProductAttribute : IEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
