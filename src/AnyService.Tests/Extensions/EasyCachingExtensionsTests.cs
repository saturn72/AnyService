using EasyCaching.Core;

namespace AnyService.Tests.Extensions
{
    public class EasyCachingExtensionsTests
    {
        [Fact]
        public async Task GetValueByPrefixAsync_ReturnsEmptyCollection_WhenEntriesNotExists()
        {
            var ecp = new Mock<IEasyCachingProvider>();
            var r = await EasyCachingExtensions.GetValueByPrefixAsync<int>(
                ecp.Object,
                "prefix",
                default);

            r.ShouldBeEmpty();
        }


        [Fact]
        public async Task GetValueByPrefixAsync_ReturnsValues()
        {
            var ecp = new Mock<IEasyCachingProvider>();
            ecp.Setup(e => e.GetByPrefixAsync<int>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, CacheValue<int>>
                {
                    { "prefix-1", new CacheValue<int>(1, true) }
                });
            var r = await EasyCachingExtensions.GetValueByPrefixAsync<int>(
                ecp.Object,
                "prefix",
                default);

            r.Count().ShouldBe(1);
        }

        [Fact]
        public async Task GetValueOrDefaultAsync_ReturnsDefaultValue_WhenKeyNotExists()
        {
            var ecp = new Mock<IEasyCachingProvider>();
            var dv = 4;
            var r = await EasyCachingExtensions.GetDefaultAsync<int>(
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
            var r = await EasyCachingExtensions.GetDefaultAsync(
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
            var r = await EasyCachingExtensions.GetDefaultAsync<int>(
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
