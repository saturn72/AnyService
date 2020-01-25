
namespace AnyService
{
    public sealed class AuthorizationInfo
    {
        public AuthorizationNode ControllerAuthorizationNode { get; set; }
        public AuthorizationNode PostAuthorizeNode { get; set; }
        public AuthorizationNode GetAuthorizeNode { get; set; }
        public AuthorizationNode PutAuthorizeNode { get; set; }
        public AuthorizationNode DeleteAuthorizeNode { get; set; }
    }
}
