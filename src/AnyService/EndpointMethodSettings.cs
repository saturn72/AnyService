using Microsoft.AspNetCore.Authorization;

namespace AnyService
{
    public sealed class EndpointMethodSettings
    {
        public bool Active { get; set; }
        public AuthorizeAttribute Authorization { get; set; }
    }
}