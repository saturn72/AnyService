using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Caching;
using AnyService.Infrastructure;
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
        public async Task ToCachedEnumerable_Async_EmptyKeyResolvesQuery(string ck)
        {
            var a = new[] { "a", "b", "c" };
            var q = Task.FromResult(a.AsQueryable());
            var res = await q.ToCachedEnumerable(ck);
            res.ShouldBe(a);
        }
        [Fact]
        public void ToCachedEnumerable_Async_GetsFromCache()
        {
            var a = new[] { "a", "b", "c" };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var eCtx = new Mock<IAppEngine>();
            eCtx.Setup(e => e.GetService<ICacheManager>()).Returns(cm.Object);
            AnyServiceAppContext.Init(eCtx.Object);

            var q = Task.FromResult(a.AsQueryable());
            var res = q.ToCachedEnumerable("ck");
            res.Result.ShouldBe(a);
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ToCachedEnumerable_Synced_EmptyKeyResolvesQuery(string ck)
        {
            var a = new[] { "a", "b", "c" };
            var q = a.AsQueryable();
            var res = await q.ToCachedEnumerable(ck);
            res.ShouldBe(a);
        }
        [Fact]
        public void ToCachedEnumerable_Synced_GetsFromCache()
        {
            var a = new[] { "a", "b", "c" };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var eCtx = new Mock<IAppEngine>();
            eCtx.Setup(e => e.GetService<ICacheManager>()).Returns(cm.Object);
            AnyServiceAppContext.Init(eCtx.Object);

            var q = a.AsQueryable();
            var res = q.ToCachedEnumerable("ck");
            res.Result.ShouldBe(a);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ToCachedCollection_Async_EmptyKeyResolvesQuery(string ck)
        {
            var a = new[] { "a", "b", "c" };
            var q = Task.FromResult(a.AsQueryable());
            var res = await q.ToCachedCollection(ck);
            res.ShouldBe(a);
        }
        [Fact]
        public void ToCachedCollection_Async_GetsFromCache()
        {
            var a = new List<string>{ "a", "b", "c" };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<Task<ICollection<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var eCtx = new Mock<IAppEngine>();
            eCtx.Setup(e => e.GetService<ICacheManager>()).Returns(cm.Object);
            AnyServiceAppContext.Init(eCtx.Object);

            var q = Task.FromResult(a.AsQueryable());
            var res = q.ToCachedCollection("ck");
            res.Result.ShouldBe(a);
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ToCachedCollection_Synced_EmptyKeyResolvesQuery(string ck)
        {
            var a = new[] { "a", "b", "c" };
            var q = a.AsQueryable();
            var res = await q.ToCachedCollection(ck);
            res.ShouldBe(a);
        }
        [Fact]
        public void ToCachedCollection_Synced_GetsFromCache()
        {
            var a = new List<string> { "a", "b", "c" };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<Task<ICollection<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var eCtx = new Mock<IAppEngine>();
            eCtx.Setup(e => e.GetService<ICacheManager>()).Returns(cm.Object);
            AnyServiceAppContext.Init(eCtx.Object);

            var q = a.AsQueryable();
            var res = q.ToCachedCollection("ck");
            res.Result.ShouldBe(a);
        }
    }
}