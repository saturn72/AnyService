using System;
using System.Threading.Tasks;
using EasyCaching.Core;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.EasyCaching.Tests
{
    public class EasyCachingCacheManagerTests
    {
        [Fact]
        public void EasyCaching_SetAsync()
        {
            var provider = new Mock<IEasyCachingProvider>();
            var config = new EasyCachingConfig();
            var cm = new EasyCachingCacheManager(provider.Object, config);
            var key = "some-key";
            var data = "some-data";
            var expiration = TimeSpan.FromSeconds(12);

            cm.Set<string>(key, data, expiration);
            provider.Verify(provider => provider.SetAsync<string>(
                    It.Is<string>(k => k == key),
                    It.Is<string>(d => d == data),
                    It.Is<TimeSpan>(ts => ts == expiration))
                , Times.Once);

            cm.Set<string>(key, data);
            provider.Verify(provider => provider.SetAsync<string>(
                    It.Is<string>(k => k == key),
                    It.Is<string>(d => d == data),
                    It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(config.DefaultCachingTimeInSeconds)))
                , Times.Once);
        }
        [Fact]
        public void EasyCaching_GetAsync()
        {
            var provider = new Mock<IEasyCachingProvider>();
            var config = new EasyCachingConfig();
            var cm = new EasyCachingCacheManager(provider.Object, config);
            var key = "key";
            var v = cm.Get<string>(key);

            provider.Verify(provider => provider.GetAsync<string>(It.Is<string>(s => s == key)), Times.Once);
        }
        [Fact]
        public async Task EasyCaching_GetAsyncWithAcquirity()
        {
            var key = "some-key";
            var data = "some-data";
            var provider = new Mock<IEasyCachingProvider>();
            provider.Setup(p => p.GetAsync<string>(It.IsAny<string>(), It.IsAny<Func<Task<string>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new CacheValue<string>(data, true));
            var config = new EasyCachingConfig();
            var cm = new EasyCachingCacheManager(provider.Object, config);
            Func<Task<string>> acquire = () => Task.FromResult(data);
            var expiration = TimeSpan.FromSeconds(12);

            var res = await cm.Get<string>(key, acquire, expiration);
            res.ShouldBe(data);
            provider.Verify(provider => provider.GetAsync<string>(
                    It.Is<string>(k => k == key),
                    It.Is<Func<Task<string>>>(d => d == acquire),
                    It.Is<TimeSpan>(ts => ts == expiration))
                , Times.Once);

            var res2 = await cm.Get<string>(key, acquire);
            res2.ShouldBe(data);
            provider.Verify(provider => provider.GetAsync<string>(
                    It.Is<string>(k => k == key),
                    It.Is<Func<Task<string>>>(d => d == acquire),
                    It.Is<TimeSpan>(ts => ts == TimeSpan.FromSeconds(config.DefaultCachingTimeInSeconds)))
                , Times.Once);
        }
        [Fact]
        public void EasyCaching_Remove()
        {
            var key = "some-key";
            var provider = new Mock<IEasyCachingProvider>();
            var config = new EasyCachingConfig();
            var cm = new EasyCachingCacheManager(provider.Object, config);
            cm.Remove(key);
            provider.Verify(provider => provider.RemoveAsync(It.Is<string>(s => s == key)), Times.Once);
        }
        [Fact]
        public void EasyCaching_Clear()
        {
            var provider = new Mock<IEasyCachingProvider>();
            var config = new EasyCachingConfig();
            var cm = new EasyCachingCacheManager(provider.Object, config);
            cm.Clear();
            provider.Verify(provider => provider.FlushAsync(), Times.Once);
        }
    }
}