using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;

namespace AnyService
{
    public sealed class EndpointSettings
    {
        /// <summary>
        /// Gets or sets value indicating whether the get operation is public to all users or just to the creator
        /// </summary>
        public bool PublicGet { get; set; }
        public Type ResponseMapperType { get; set; }
        public AuthorizeAttribute Authorization { get; set; }
        public Type ControllerType { get; set; }
        public string Area { get; set; }
        /// <summary>
        /// Gets or sets route for entity.
        /// </summary>
        public PathString Route { get; set; }
        public Type MapToType { get; set; }
        public Type MapToPaginationType { get; set; }
        public EndpointMethodSettings PostSettings { get; set; }
        public EndpointMethodSettings GetSettings { get; set; }
        public EndpointMethodSettings PutSettings { get; set; }
        public EndpointMethodSettings DeleteSettings { get; set; }
        public bool Disabled { get; set; }
    }
}