using Xunit;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AnyService.Services.FileStorage;
using System.IO;
using System.Linq;
using Shouldly;

namespace AnyService.EntityFramework.Tests
{
    public class EfFileStorageManagerTests
    {
        private readonly TestDbContext _dbContext;
        private readonly EfFileStoreManager _fileManager;
        private static readonly DbContextOptions<TestDbContext> DbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "test_ef_db_files")
            .Options;
        public EfFileStorageManagerTests()
        {
            _dbContext = new TestDbContext(DbOptions);
            _fileManager = new EfFileStoreManager(_dbContext);

        }
        [Fact]
        public async Task UploadFiles()
        {
            var fileName1 = "dog.jpg";
            var fileName2 = "video.mp4";

            FileModel f1 = new FileModel
            {
                Bytes = await File.ReadAllBytesAsync("./resources/" + fileName1),
                DisplayFileName = fileName1,
            },
            f2 = new FileModel
            {
                Bytes = await File.ReadAllBytesAsync("./resources/" + fileName2),
                DisplayFileName = fileName2,
            };

            await _dbContext.Set<FileModel>().AddAsync(f1);
            await _dbContext.SaveChangesAsync();

            var fms = new[] { f1, f2 };
            var res = await _fileManager.Upload(fms);

            res.Count().ShouldBe(fms.Length);
            for (int i = 0; i < fms.Length; i++)
            {
                var curRef = fms[i];
                var curRes = res.ElementAt(i);
                curRes.File.ShouldBe(curRef);
                curRes.File.StoredFileName.ShouldBe(curRef.DisplayFileName);
                curRes.Status.ShouldBe(FileStoreState.Uploaded);
            }
        }
    }
}
