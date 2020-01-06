
using Microsoft.AspNetCore.Authorization;

namespace AnyService
{
    public sealed class AuthorizationInfo
    {
        public AuthorizeAttribute ControllerAuthorizeAttribute { get; set; }
        public AuthorizeAttribute PostAuthorizeAttribute { get; set; }
        public AuthorizeAttribute GetAuthorizeAttribute { get; set; }
        public AuthorizeAttribute PutAuthorizeAttribute { get; set; }
        public AuthorizeAttribute DeleteAuthorizeAttribute { get; set; }
    }
}
