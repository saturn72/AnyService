using EasyCaching.Core;

namespace AnyService.Tests.Extensions
{
    public class EasyCachingExtensionsTests
    {
        [Fact]
        public async Task GetValueOrDefaultAsync_ReturnsDefaultValue_WhenKeyNotExists()
        {
            var ecp = new Mock<IEasyCachingProvider>();
            var dv = 4;
            var r = await EasyCachingExtensions.GetValueOrDefaultAsync<int>(
                ecp.Object,
                "not-exists",
                dv,
                default);

            r.ShouldBe(dv);
        }
        [Fact]
        public async Task GetValueOrDefaultAsync_ReturnsDefaultValue_WhenCachedValueHasNoValue()
        {
            int v = 44,
               dv = 766;
            var ecp = new Mock<IEasyCachingProvider>();
            ecp.Setup(e => e.GetAsync<int>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CacheValue<int>(v, false));
            var r = await EasyCachingExtensions.GetValueOrDefaultAsync<int>(
                ecp.Object,
                "exists",
                dv,
                default);

            r.ShouldBe(dv);
        }

        [Fact]
        public async Task GetValueOrDefaultAsync_ReturnsValue_WhenKeyExists()
        {
            int v = 44,
                dv = 766;
            var ecp = new Mock<IEasyCachingProvider>();
            ecp.Setup(e => e.GetAsync<int>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CacheValue<int>(v, true));
            var r = await EasyCachingExtensions.GetValueOrDefaultAsync<int>(
                ecp.Object,
                "exists",
                dv,
                default);

            r.ShouldBe(v);
        }

        [Fact]
        public async Task TryGetValueAsync_ReturnsFalse_WhenItemNotCached()
        {
            var ecp = new Mock<IEasyCachingProvider>();

            var (b, v) = await EasyCachingExtensions.TryGetValueAsync<int>(
                ecp.Object,
                "not-exists",
                default);

            b.ShouldBeFalse();
            v.ShouldBe(default);
        }

        [Fact]
        public async Task TryGetValueAsync_ReturnsFalse_WhenItemHasNoValue()
        {
            var x = 44;
            var ecp = new Mock<IEasyCachingProvider>();
            ecp.Setup(e => e.GetAsync<int>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CacheValue<int>(x, false));

            var (b, v) = await EasyCachingExtensions.TryGetValueAsync<int>(
                ecp.Object,
                "not-exists",
                default);

            b.ShouldBeFalse();
            v.ShouldBe(default);
        }

        [Fact]
        public async Task TryGetValueAsync_ReturnsTrue_AndValue_WhenItemValue()
        {
            var x = 44;
            var ecp = new Mock<IEasyCachingProvider>();
            ecp.Setup(e => e.GetAsync<int>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CacheValue<int>(x, true));

            var (b, v) = await EasyCachingExtensions.TryGetValueAsync<int>(
                ecp.Object,
                "not-exists",
                default);

            b.ShouldBeTrue();
            v.ShouldBe(x);
        }
    }
}
