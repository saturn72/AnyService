using Microsoft.AspNetCore.Http;
using System;

namespace AnyService
{
    public sealed class ControllerSettings
    {
        /// <summary>
        /// Gets or sets value indicating whether the get operation is public to all users or just to the creator
        /// </summary>
        public bool PublicGet { get; set; }
        public Type ResponseMapperType { get; set; }
        public AuthorizationInfo Authorization { get; set; }
        public Type ControllerType { get; set; }
        public string Area { get; set; }
        /// <summary>
        /// Gets or sets route for entity.
        /// </summary>
        public PathString Route { get; set; }
        public Type MapToType { get; set; }
        public Type MapToPaginationType { get; set; }
        public ControllerMethodSettings PostSettings { get; set; }
        public ControllerMethodSettings GetSettings { get; set; }
        public ControllerMethodSettings PutSettings { get; set; }
        public ControllerMethodSettings DeleteSettings { get; set; }
        public bool Active { get; set; } = true;
    }
}