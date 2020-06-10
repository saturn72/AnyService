using AnyService.Services.FileStorage;
using Shouldly;
using Xunit;

namespace AnyService.Services.Tests.FileStorage
{
    public class FileUploadResponseTests
    {
        [Fact]
        public void FileUploadResponseInit()
        {
            new FileStorageResponse().Status.ShouldBe(FileStoreState.NotSet);
        }
    }
}