using System;
using AnyService.Audity;
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
            var ah = new AuditHelper(null);

            AuditHelperExtensions.PrepareForCreate(ah, a, userId);

            a[0].CreatedByUserId.ShouldBe(userId);
            a[0].CreatedOnUtc.ShouldBeGreaterThan(null);
            a[0].CreatedOnUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK"));
        }
    }
}