using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using AnyService.Services.FileStorage;
using System;

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
            var toUpdate = new List<FileModel>();
            var toCreate = new List<FileModel>();

            foreach (var f in files)
            {
                f.StoredFileName = f.DisplayFileName;

                if (f.Id.HasValue())
                    toUpdate.Add(f);
                else
                {
                    toCreate.Add(f);
                }
            }

            await Task.Run(() => dbSet.UpdateRange(toUpdate));
            await dbSet.AddRangeAsync(toCreate);
            await _dbContext.SaveChangesAsync();

            return files.Select(f => new FileUploadResponse { File = f, Status = UploadStatus.Uploaded }).ToArray();
        }
    }
}