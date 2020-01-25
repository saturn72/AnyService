using System;
using System.Collections.Generic;

namespace AnyService.Services.Security
{
    public class PermissionFuncs
    {
        public static Func<TypeConfigRecord, string> GetByHttpMethod(string method) => HttpMethodToPerMissionKey[method];
        private static readonly IReadOnlyDictionary<string, Func<TypeConfigRecord, string>> HttpMethodToPerMissionKey = new Dictionary<string, Func<TypeConfigRecord, string>>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "POST" ,t => t.PermissionRecord.CreateKey},
            {  "GET",t  => t.PermissionRecord.ReadKey},
            {   "PUT",t  => t.PermissionRecord.UpdateKey},
            {   "DELETE", t => t.PermissionRecord.DeleteKey },
        };
    }
}
