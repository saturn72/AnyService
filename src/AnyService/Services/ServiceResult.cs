using System.Collections.Generic;

namespace AnyService.Services
{
    public sealed class ServiceResult
    {
        public const string Accepted = "accepted";
        public const string BadOrMissingData = "bad-or-missing-data";
        public const string Error = "error";
        public const string NotFound = "not-found";
        public const string NotSet = "not-set";
        public const string Ok = "ok";
        public const string Unauthorized = "unauthorized";

        public static IEnumerable<string> All => new[]{
            Accepted,
            BadOrMissingData ,
            Error ,
            NotFound ,
            NotSet ,
            Ok ,
            Unauthorized,
        };
    }
}