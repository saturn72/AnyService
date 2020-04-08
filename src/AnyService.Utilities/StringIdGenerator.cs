using System;
using System.Threading.Tasks;

namespace AnyService.Utilities
{
    public sealed class StringIdGenerator : IIdGenerator
    {
        private static object lockObj = new object();
        private static string GenerateId()
        {
            var guid = Guid.NewGuid();
            string uuid = Convert.ToBase64String(guid.ToByteArray());
            uuid = uuid.Replace("=", "").Replace("+", "");
            return uuid;
        }

        public object GetNext()
        {
            lock (lockObj)
            {
                return GenerateId();
            }
        }
    }
}