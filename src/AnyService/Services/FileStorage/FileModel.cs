using System.Collections.Generic;
using System.IO;

namespace AnyService.Services.FileStorage
{
    public sealed class FileModel : ChildModelBase
    {
        public string BucketName { get; set; }
        public string FileName { get; set; }
        public IEnumerable<byte> Bytes { get; set; }
        public Stream Stream { get; set; }
    }
}