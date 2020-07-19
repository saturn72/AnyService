using System;
using AnyService.Audity;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Audity
{
    public class AuditHelperExtensionsTests
    {
        [Fact]
        public void PrepareForCreate()
        {
            var userId = "some-user-id";
            var a = new[] { new MyAudity() };
            var wc = new WorkContext();
            var sp = new Mock<IServiceProvider> ();
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            var ah = new AuditHelper(sp.Object);

            AuditHelperExtensions.PrepareForCreate(ah, a, userId);

            a[0].CreatedByUserId.ShouldBe(userId);
            a[0].CreatedOnUtc.ShouldBeGreaterThan(null);
            a[0].CreatedOnUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK"));
            a[0].CreatedWorkContextJson.ShouldBe(wc.Parameters.ToJsonString());
        }
    }
}