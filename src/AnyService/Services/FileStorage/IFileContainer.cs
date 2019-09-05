using System.Collections.Generic;

namespace AnyService.Services.FileStorage
{
    public interface IFileContainer : IDomainModelBase
    {
        IEnumerable<FileModel> Files { get; set; }
    }
}