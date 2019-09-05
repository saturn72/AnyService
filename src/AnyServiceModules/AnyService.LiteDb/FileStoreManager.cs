using System.Collections.Generic;
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
                    var lfi = db.FileStorage.Upload(f.Id, f.FileName, f.Stream);
                    furList.Add(new FileUploadResponse { File = f, Status = UploadStatus.Uploaded });
                }
            }));

            return furList;
        }
    }
}
