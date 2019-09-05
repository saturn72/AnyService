using System.Collections.Generic;

namespace AnyService.Services.FileStorage
{
    public sealed class FileModel : ChildModelBase
    {
        public string BucketName { get; set; }
        public string FileName { get; set; }
        public IEnumerable<byte> Bytes { get; set; }
    }
}