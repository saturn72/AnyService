using System.Collections.Generic;
using System.IO;

namespace AnyService.Services
{
    public interface IFileContainer : IDomainModelBase
    {
        IEnumerable<FileModel> Files { get; set; }
    }

    public sealed class FileModel : IDomainModelBase
    {
        public string ContainerKey { get; set; }
        public string ContainerId { get; set; }
        public string Id { get; set; }
        public string FileName { get; set; }
        public IEnumerable<byte> Bytes { get; set; }
        public Stream Stream { get; set; }
    }
}