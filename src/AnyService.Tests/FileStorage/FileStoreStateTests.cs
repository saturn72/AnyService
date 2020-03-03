using System.Linq;
using AnyService.Services.FileStorage;
using Shouldly;
using Xunit;

namespace AnyService.Services.Tests.FileStorage
{
    public class FileStoreStateTests
    {
        [Fact]
        public void VerifyallKeys()
        {
            FileStoreState.All.Count().ShouldBe(7);
            FileStoreState.NotSet.ShouldBe("notset");
            FileStoreState.UploadPending.ShouldBe("uploadpending");
            FileStoreState.Uploaded.ShouldBe("uploaded");
            FileStoreState.UploadFailed.ShouldBe("uploadfailed");

            FileStoreState.DeletePending.ShouldBe("deletepending");
            FileStoreState.Deleted.ShouldBe("deleted");
            FileStoreState.DeleteFailed.ShouldBe("deletefailed");
        }
    }
}