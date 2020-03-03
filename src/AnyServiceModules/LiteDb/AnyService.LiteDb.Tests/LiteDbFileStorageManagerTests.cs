using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AnyService.Services.FileStorage;
using LiteDB;
using Shouldly;
using Xunit;

namespace AnyService.LiteDb.Tests
{
    public class LiteDbFileStorageManagerTests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetCurrentMethodName()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);

            return sf.GetMethod().Name;
        }
        [Fact]
        public async Task UploadTests()
        {
            var dbName = $"testdb-{GetCurrentMethodName()}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";
            var fsm = new LiteDbFileStoreManager(dbName);

            var file1 = new FileModel
            {
                Id = "1",
                Bytes = File.ReadAllBytes(@"resources\file.txt"),
                StoredFileName = "file.txt"
            };
            var file2 = new FileModel
            {
                Id = "2",
                Bytes = File.ReadAllBytes(@"resources\file.csv"),
                StoredFileName = "file.csv"
            };

            var files = new[] { file1, file2 };
            var res = await fsm.Upload(files);
            res.Count().ShouldBe(files.Count());
            for (int i = 0; i < files.Length; i++)
            {
                var c = res.ElementAt(i);
                c.Status.ShouldBe(FileStoreState.Uploaded);
                c.File.ShouldBe(files[i]);
            }
        }

        [Fact]
        public async Task Delete_Tests()
        {
            var dbName = $"testdb-{GetCurrentMethodName()}-{DateTime.UtcNow.ToString("yyyy-mm-dd_hh-mm-dd-fff")}.db";
            var fsm = new LiteDbFileStoreManager(dbName);

            var file1 = new FileModel
            {
                Id = "1",
                Bytes = File.ReadAllBytes(@"resources\file.txt"),
                StoredFileName = "file.txt"
            };
            var file2 = new FileModel
            {
                Id = "2",
                Bytes = File.ReadAllBytes(@"resources\file.csv"),
                StoredFileName = "file.csv"
            };
            var files = new[] { file1, file2 };
            using (var db = new LiteDatabase(dbName))
            {
                foreach (var f in files)
                {
                    var stream = new MemoryStream(f.Bytes.ToArray()) as Stream;
                    db.FileStorage.Upload(f.Id, f.StoredFileName, stream);

                }
            }

            var res = await fsm.Delete(files);
            res.Count().ShouldBe(files.Count());
            for (int i = 0; i < files.Length; i++)
            {
                var c = res.ElementAt(i);
                c.Status.ShouldBe(FileStoreState.Deleted);
                c.File.ShouldBe(files[i]);
            }
        }

    }
}
