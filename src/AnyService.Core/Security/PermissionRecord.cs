namespace AnyService.Core.Security
{
    public sealed class PermissionRecord
    {
        public PermissionRecord(string createPermissionKey, string readPermissionKey, string updatePermissionKey, string deletePermissionKey, PermissionStyle createPermissionStyle = PermissionStyle.Optimistic)
        {
            CreateKey = createPermissionKey;
            ReadKey = readPermissionKey;
            UpdateKey = updatePermissionKey;
            DeleteKey = deletePermissionKey;
            CreatePermissionStyle = createPermissionStyle;
        }

        public string CreateKey { get; }
        public string ReadKey { get; }
        public string UpdateKey { get; }
        public string DeleteKey { get; }
        public PermissionStyle CreatePermissionStyle { get; }
    }
}