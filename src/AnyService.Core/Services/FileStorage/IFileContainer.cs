using System.Collections.Generic;

namespace AnyService.Services.FileStorage
{
    public interface IFileContainer : IDomainEntity
    {
        IEnumerable<FileModel> Files { get; set; }
    }
}