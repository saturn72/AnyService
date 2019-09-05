namespace AnyService.Services.FileStorage
{
    public sealed class FileUploadResponse
    {
        public FileUploadResponse()
        {
            Status = UploadStatus.NotSet;
        }
        public string Status { get; set; }
        public FileModel File { get; set; }
        public string Message { get; set; }
    }
}