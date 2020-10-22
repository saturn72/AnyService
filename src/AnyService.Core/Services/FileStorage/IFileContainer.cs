using System.Collections.Generic;

namespace AnyService.Services.FileStorage
{
    public interface IFileContainer : IEntity
    {
        IEnumerable<FileModel> Files { get; set; }
    }
}