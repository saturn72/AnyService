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
        public async Task<IEnumerable<FileStorageResponse>> Upload(IEnumerable<FileModel> files)
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

            return files.Select(f => new FileStorageResponse { File = f, Status = FileStoreState.Uploaded }).ToArray();
        }

        public async Task<IEnumerable<FileStorageResponse>> Delete(IEnumerable<FileModel> files)
        {
            var dbSet = _dbContext.Set<FileModel>();
            var ids = files.Select(f => f.Id).ToArray();

            var toDelete = dbSet.Where(e => ids.Contains(e.Id, StringComparer.InvariantCultureIgnoreCase));
            await Task.Run(() => dbSet.RemoveRange(toDelete));
            await _dbContext.SaveChangesAsync();

            return files.Select(f => new FileStorageResponse { File = f, Status = FileStoreState.Deleted }).ToArray();
        }
    }
}