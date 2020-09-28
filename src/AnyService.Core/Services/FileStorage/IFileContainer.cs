using System.Collections.Generic;

namespace AnyService.Services.FileStorage
{
    public interface IFileContainer : IDomainObject
    {
        IEnumerable<FileModel> Files { get; set; }
    }
}