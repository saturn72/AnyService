using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Services.FileStorage;

namespace AnyService.LiteDb
{
    public class FileStoreManager : IFileStoreManager
    {
        private readonly string _dbName;

        public FileStoreManager(string dbName)
        {
            _dbName = dbName;
        }
        public async Task<IEnumerable<FileUploadResponse>> Upload(IEnumerable<FileModel> files)
        {
            var furList = new List<FileUploadResponse>();
            await Task.Run(() => LiteDbUtility.Command(_dbName, db =>
            {
                foreach (var f in files)
                {
                    using (var stream = new MemoryStream(f.Bytes.ToArray()))
                    {
                        var lfi = db.FileStorage.Upload(f.Id, f.FileName, stream);
                        furList.Add(new FileUploadResponse { File = f, Status = UploadStatus.Uploaded });
                    }
                }
            }));

            return furList;
        }
    }
}
