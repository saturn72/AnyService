using System.Collections.Generic;

namespace AnyService
{
    public sealed class AuthorizationNode
    {
        public IEnumerable<string> Roles { get; set; }
    }
}