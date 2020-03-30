
namespace AnyService
{
    public sealed class AuthorizationInfo
    {
        public AuthorizationNode ControllerAuthorizationNode { get; set; }
        public AuthorizationNode PostAuthorizationNode { get; set; }
        public AuthorizationNode GetAuthorizationNode { get; set; }
        public AuthorizationNode PutAuthorizationNode { get; set; }
        public AuthorizationNode DeleteAuthorizationNode { get; set; }
    }
}
