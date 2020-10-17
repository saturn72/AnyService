using AnyService.Services;
using System.Collections.Generic;

namespace AnyService.SampleApp.Entities
{
    public class Category : IDomainEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AdminComment { get; set; }
        [Aggregated("product")]
        public IEnumerable<Product> Products { get; set; }
    }
}
