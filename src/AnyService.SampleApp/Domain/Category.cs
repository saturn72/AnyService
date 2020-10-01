namespace AnyService.SampleApp.Domain
{
    public class Category : IDomainEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AdminComment { get; set; }
    }
}
