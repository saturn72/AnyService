namespace AnyService.Services.FileStorage
{
    public sealed class FileStorageResponse
    {
        public FileStorageResponse()
        {
            Status = FileStoreState.NotSet;
        }
        public string Status { get; set; }
        public FileModel File { get; set; }
        public string Message { get; set; }
    }
}