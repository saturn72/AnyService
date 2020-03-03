using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Services.FileStorage;

namespace AnyService.LiteDb
{
    public class LiteDbFileStoreManager : IFileStoreManager
    {
        private readonly string _dbName;

        public LiteDbFileStoreManager(string dbName)
        {
            _dbName = dbName;
        }

        public async Task<IEnumerable<FileStorageResponse>> Delete(IEnumerable<FileModel> files)
        {
            var furList = new List<FileStorageResponse>();
            await Task.Run(() => LiteDbUtility.Command(_dbName, db =>
            {
                foreach (var f in files)
                {
                    var s = db.FileStorage.Delete(f.Id);
                    furList.Add(new FileStorageResponse
                    {
                        File = f,
                        Status = s ? FileStoreState.Deleted : FileStoreState.DeleteFailed
                    });
                }
            }));

            return furList;
        }

        public async Task<IEnumerable<FileStorageResponse>> Upload(IEnumerable<FileModel> files)
        {
            var furList = new List<FileStorageResponse>();
            await Task.Run(() => LiteDbUtility.Command(_dbName, db =>
            {
                foreach (var f in files)
                {
                    using (var stream = f.TempPath.HasValue()
                        ? File.OpenRead(f.TempPath)
                        : new MemoryStream(f.Bytes.ToArray()) as Stream)
                    {
                        var lfi = db.FileStorage.Upload(f.Id, f.StoredFileName, stream);
                        furList.Add(new FileStorageResponse { File = f, Status = FileStoreState.Uploaded });
                    }
                }
            }));

            return furList;
        }
    }
}
