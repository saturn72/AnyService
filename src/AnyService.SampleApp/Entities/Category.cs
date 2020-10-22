namespace AnyService.SampleApp.Entities
{
    public class Category : IEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AdminComment { get; set; }
    }
}
