using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Caching;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Core.Tests.Caching
{
    public class CacheManagerExtensionsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ToCachedEnumerable_Async_EmptyKeyResolvesQuery(string ck)
        {
            var a = new[] { "a", "b", "c" };
            var q = Task.FromResult(a.AsQueryable());
            var cm = new Mock<ICacheManager>();
            var res = await CacheManagerExtensions.ToCachedCollection(cm.Object, q,ck, TimeSpan.FromDays(1));
            res.ShouldBe(a);
        }
        [Fact]
        public async Task ToCachedEnumerable_Async_GetsFromCache()
        {
            var a = new[] { "a", "b", "c" };
            var ck = "cKey";
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var q = Task.FromResult(a.AsQueryable());
            var res = await CacheManagerExtensions.ToCachedEnumerable(cm.Object, q, ck, TimeSpan.FromDays(1));
            res.ShouldBe(a);
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ToCachedEnumerable_Synced_EmptyKeyResolvesQuery(string ck)
        {
            var a = new[] { "a", "b", "c" };
            var q = a.AsQueryable();
            var cm = new Mock<ICacheManager>();
            var res = await CacheManagerExtensions.ToCachedEnumerable(cm.Object, q, ck, TimeSpan.FromDays(1));
            res.ShouldBe(a);
        }
        [Fact]
        public async Task ToCachedEnumerable_Synced_GetsFromCache()
        {
            var ck = "cKey";
            var a = new[] { "a", "b", "c" };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var q = a.AsQueryable();
            var res = await CacheManagerExtensions.ToCachedEnumerable(cm.Object, q, ck, TimeSpan.FromDays(1));
            res.ShouldBe(a);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ToCachedCollection_Async_EmptyKeyResolvesQuery(string ck)
        {
            var a = new[] { "a", "b", "c" };
            var q = Task.FromResult(a.AsQueryable());
            var cm = new Mock<ICacheManager>();
            var res = await CacheManagerExtensions.ToCachedCollection(cm.Object, q, ck, TimeSpan.FromDays(1));
            res.ShouldBe(a);
        }
        [Fact]
        public async Task ToCachedCollection_Async_GetsFromCache()
        {
            var ck = "cKey";
            var a = new List<string>{ "a", "b", "c" };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<Task<ICollection<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var q = Task.FromResult(a.AsQueryable());
            var res = await CacheManagerExtensions.ToCachedCollection(cm.Object, q, ck, TimeSpan.FromDays(1));

            res.ShouldBe(a);
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ToCachedCollection_Synced_EmptyKeyResolvesQuery(string ck)
        {
            var cm = new Mock<ICacheManager>();
            var a = new[] { "a", "b", "c" };
            var q = a.AsQueryable();
            var res = await CacheManagerExtensions.ToCachedCollection(cm.Object, q, ck, TimeSpan.FromDays(1));
            res.ShouldBe(a);
        }
        [Fact]
        public async Task ToCachedCollection_Synced_GetsFromCache()
        {
            var ck = "cKey";
            var a = new List<string> { "a", "b", "c" };
            var cm = new Mock<ICacheManager>();
            cm.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<Task<ICollection<string>>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(a);

            var q = a.AsQueryable();
            var res = await CacheManagerExtensions.ToCachedCollection(cm.Object, q, ck, TimeSpan.FromDays(1));
            res.ShouldBe(a);
        }
    }
}