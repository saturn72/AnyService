using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AnyService.Tests.Extensions
{
    public class DistributedCacheExtensionsTests
    {

        [Fact]
        public async Task GetAsyncGeneric_ReturnsDefaultWhenNotExists()
        {
            var x = 44;
            var chace = new Mock<IDistributedCache>();
            chace.Setup(e => e.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(byte[]));

            var v = await DistributedCacheExtensionsClass.GetAsync<int>(chace.Object, "test");
            v.ShouldBe(default);
        }
        [Fact]
        public async Task GetAsyncGeneric_ReturnsFromCache()
        {
            var x = 44;
            var cache = new Mock<IDistributedCache>();
            cache.Setup(e => e.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(x));

            var v = await DistributedCacheExtensionsClass.GetAsync<int>(cache.Object, "not-exists");

            v.ShouldBe(x);
        }

        [Fact]
        public async Task GetAsyncGeneric_WithAcquirarReturnsFromCache()
        {
            var x = 44;
            var cache = new Mock<IDistributedCache>();
            cache.Setup(e => e.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(JsonSerializer.SerializeToUtf8Bytes(x));

            var v = await DistributedCacheExtensionsClass.GetAsync<int>(cache.Object, "not-exists");

            v.ShouldBe(x);
        }

        [Fact]
        public async Task GetAsyncGeneric_WithAcquirar_ReturnsFromAcquirar()
        {
            var x = 51;
            var ts = TimeSpan.FromMicroseconds(1);
            var key = "not-exists";
            var cache = new Mock<IDistributedCache>();
            cache.Setup(e => e.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(default(byte[]));

            var v = await DistributedCacheExtensionsClass.GetAsync(cache.Object, key, () => Task.FromResult(x), ts);

            v.ShouldBe(x);
            var expBytes = JsonSerializer.SerializeToUtf8Bytes(x);
            cache.Verify(c => c.SetAsync(
                It.Is<string>(k => k == key),
                It.Is<byte[]>(b => b.Length == expBytes.Length && b[0] == expBytes[0] && b[1] == expBytes[1]),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == ts),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

    }
}
