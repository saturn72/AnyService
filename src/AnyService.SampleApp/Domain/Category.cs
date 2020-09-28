namespace AnyService.SampleApp.Domain
{
    public class Category : IDomainObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AdminComment { get; set; }
    }
}
