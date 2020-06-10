using System.Collections.Generic;

namespace AnyService.Services.FileStorage
{
    public sealed class FileApiModel : ChildModelBase
    {
        public IEnumerable<byte> Bytes { get; set; }
        public string FileName { get; set; }
    }
    public sealed class FileModel : ChildModelBase
    {
        public string TempPath { get; set; }
        public byte[] Bytes { get; set; }
        public string DisplayFileName { get; set; }
        public string UntrustedFileName { get; set; }
        public string StoredFileName { get; set; }
    }
}