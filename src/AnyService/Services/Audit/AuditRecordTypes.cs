using System.Collections.Generic;

namespace AnyService.Services.Audit
{
    public class AuditRecordTypes
    {
        public const string CREATE = "create";
        public const string READ = "read";
        public const string UPDATE = "update";
        public const string DELETE = "delete";

        public static IEnumerable<string> All => new[]
        {
            CREATE,
            READ,
            UPDATE,
            DELETE
        };
    }
}
