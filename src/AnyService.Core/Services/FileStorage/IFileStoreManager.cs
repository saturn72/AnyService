using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services.FileStorage
{
    public interface IFileStoreManager
    {
        Task<IEnumerable<FileStorageResponse>> Upload(IEnumerable<FileModel> files);
        Task<IEnumerable<FileStorageResponse>> Delete(IEnumerable<FileModel> files);
    }
}