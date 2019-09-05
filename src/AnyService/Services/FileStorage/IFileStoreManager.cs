using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services.FileStorage
{
    public interface IFileStoreManager
    {
        Task<IEnumerable<FileUploadResponse>> Upload(IEnumerable<FileModel> files);
    }
}