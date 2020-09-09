using AnyService;

namespace API.Domain
{
    public class Product : IDomainModelBase
    {
        public string Id {get;set;}
        public string Name { get; set; }
    }
}
