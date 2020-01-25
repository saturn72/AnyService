using System.Collections.Generic;
using AnyService.Core;

namespace AnyService.Services.FileStorage
{
    public interface IFileContainer : IDomainModelBase
    {
        IEnumerable<FileModel> Files { get; set; }
    }
}