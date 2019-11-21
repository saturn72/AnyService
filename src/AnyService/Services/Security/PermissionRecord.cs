namespace AnyService.Services.Security
{
    public sealed class PermissionRecord
    {
        public PermissionRecord(string createPermissionKey, string readPermissionKey, string updatePermissionKey, string deletePermissionKey)
        {
            CreateKey = createPermissionKey;
            ReadKey = readPermissionKey;
            UpdateKey = updatePermissionKey;
            DeleteKey = deletePermissionKey;
        }

        public string CreateKey { get; }
        public string ReadKey { get; }
        public string UpdateKey { get; }
        public string DeleteKey { get; }
    }
}