using Microsoft.AspNetCore.Authorization;

namespace AnyService.Settings
{
    public sealed class EndpointMethodSettings
    {
        public bool Active { get; set; }
        public AuthorizeAttribute Authorization { get; set; }
    }
}