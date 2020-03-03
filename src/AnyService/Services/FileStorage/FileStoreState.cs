using System.Collections.Generic;

namespace AnyService.Services.FileStorage
{
    public class FileStoreState
    {
        public const string NotSet = "notset";
        public const string UploadPending = "uploadpending";
        public const string Uploaded = "uploaded";
        public const string UploadFailed = "uploadfailed";
        public const string DeletePending = "deletepending";
        public const string Deleted = "deleted";
        public const string DeleteFailed = "deletefailed";
        public static IEnumerable<string> All => new[]{
            NotSet,
            UploadPending,
            Uploaded,
            UploadFailed,
            DeletePending,
            Deleted,
            DeleteFailed
        };
    }
}