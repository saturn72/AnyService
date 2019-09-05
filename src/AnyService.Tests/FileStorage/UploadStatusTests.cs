using System.Linq;
using AnyService.Services.FileStorage;
using Shouldly;
using Xunit;

namespace AnyService.Services.Tests.FileStorage
{
    public class UploadStatusTests
    {
        [Fact]
        public void VerifyallKeys()
        {
            UploadStatus.All.Count().ShouldBe(4);
            UploadStatus.NotSet.ShouldBe("notset");
            UploadStatus.UploadPending.ShouldBe("uploadpending");
            UploadStatus.Uploaded.ShouldBe("uploaded");
            UploadStatus.UploadFailed.ShouldBe("uploadfailed");
        }
    }
}