using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using AnyService.Services.FileStorage;

namespace AnyService.EntityFramework
{
    public class EfFileStoreManager : IFileStoreManager
    {
        private readonly DbContext _dbContext;

        public EfFileStoreManager(DbContext dbContext)
        {

            _dbContext = dbContext;
        }
        public async Task<IEnumerable<FileUploadResponse>> Upload(IEnumerable<FileModel> files)
        {
            var dbSet = _dbContext.Set<FileModel>();
            await dbSet.AddRangeAsync(files);
            await _dbContext.SaveChangesAsync();

            var res = files.Select(f => new FileUploadResponse { File = f, Status = UploadStatus.Uploaded }).ToArray();
            return res;
        }
    }
}