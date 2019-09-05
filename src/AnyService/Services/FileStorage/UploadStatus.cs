using System.Collections.Generic;

namespace AnyService.Services.FileStorage
{
    public class UploadStatus
    {
        public const string NotSet = "notset";
        public const string UploadPending = "uploadpending";
        public const string Uploaded = "uploaded";
        public const string UploadFailed = "uploadfailed";
        public static IEnumerable<string> All => new[]{
            NotSet,
            UploadPending,
            Uploaded,
            UploadFailed,
        };
    }
}