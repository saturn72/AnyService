using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Caching;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Utilities
{
    public class QueryableExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ToCached_EmptyKeyResolvesQuery(string ck)
        {
            var a = new[] { "a", "b", "c" };
            var q = a.AsQueryable();
            var res = await q.ToCachedCollection(ck);
            res.ShouldBe(a);
        }
        [Fact]
        public void ToCached_GetsFromCache()
        {
            var a = new[] { "a", "b", "c" };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get<IEnumerable<string>>(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(It.Is<Type>(t => t == typeof(ICacheManager)))).Returns(cm.Object);
            AppEngine.Init(sp.Object);

            var q = a.AsQueryable();
            var res = q.ToCachedCollection("ck");
            res.Result.ShouldBe(a);
        }
    }
}