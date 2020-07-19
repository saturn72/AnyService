using System;
using System.Collections.Generic;
using System.Linq;
using AnyService.Audity;
using Moq;
using Shouldly;
using Xunit;

namespace AnyService.Tests.Audity
{
    public class AudityHelperTests
    {
        [Fact]
        public void PrepareForCreateTest()
        {
            var userId = "some-user-id";
            var a = new MyAudity();
            var wc = new WorkContext();
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            var ah = new AuditHelper(sp.Object);

            ah.PrepareForCreate(a, userId);

            a.CreatedByUserId.ShouldBe(userId);
            a.CreatedOnUtc.ShouldBeGreaterThan(null);
            a.CreatedOnUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK"));
        }
        [Fact]
        public void PrepareForUpdateTest()
        {
            var userId = "some-user-id";
            var dbModel = new MyAudity
            {
                CreatedByUserId = "123",
                CreatedOnUtc = "123123"
            };

            var a = new MyAudity();

            var wc = new WorkContext();
            var sp = new Mock<IServiceProvider>();
            sp.Setup(s => s.GetService(typeof(WorkContext))).Returns(wc);

            var ah = new AuditHelper(sp.Object);

            ah.PrepareForUpdate(dbModel, a, userId);

            var uRec = a.UpdateRecords.First();
            uRec.UpdatedByUserId.ShouldBe(userId);
            uRec.UpdatedOnUtc.ShouldBeGreaterThan(null);
            uRec.UpdatedOnUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK"));
            uRec.WorkContextJson.ShouldBe(wc.Parameters.ToJsonString());

            a.CreatedByUserId.ShouldBe(dbModel.CreatedByUserId);
            a.CreatedOnUtc.ShouldBe(dbModel.CreatedOnUtc);
        }
        [Fact]
        public void PrepareForDeleteTest()
        {
            var userId = "some-user-id";
            var a = new MyAudity();
            var ah = new AuditHelper(null);

            ah.PrepareForDelete(a, userId);

            a.Deleted.ShouldBeTrue();
            a.DeletedByUserId.ShouldBe(userId);
            a.DeletedOnUtc.ShouldBeGreaterThan(null);
            a.DeletedOnUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssK"));
        }
    }

    public class MyAudity : IFullAudit
    {
        public string CreatedOnUtc { get; set; }
        public string CreatedByUserId { get; set; }
        public string CreatedWorkContextJson { get; set; }
        public IEnumerable<UpdateRecord> UpdateRecords { get; set; }
        public bool Deleted { get; set; }
        public string DeletedOnUtc { get; set; }
        public string DeletedByUserId { get; set; }
    }
}