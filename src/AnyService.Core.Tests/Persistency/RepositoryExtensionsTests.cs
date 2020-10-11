using AnyService.Services;
using Moq;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AnyService.Core.Tests.Persistency
{
    public class RepositoryExtensionsTests
    {
        public class MyClass : IDomainEntity
        {
            public string Id { get; set; }
        }
        [Fact]
        public async Task GetBy()
        {
            var col = new[]
            {
                new MyClass
                {
                    Id = "a",
                },
                new MyClass
                {
                    Id = "A",
                },
                new MyClass
                {
                    Id = "a",
                },
                new MyClass
                {
                    Id = "b",
                },
            };
            var repo = new Mock<IRepository<MyClass>>();
            repo.Setup(r => r.Collection).ReturnsAsync(col.AsQueryable());
            var res = await RepositoryExtensions.GetBy(repo.Object, x => x.Id == "a");
            var arr = res.ToArray();
            arr.Length.ShouldBe(2);
            arr.ShouldContain(x => x == col.ElementAt(0) || x == col.ElementAt(2));
        }
    }
}
