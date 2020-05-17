using System;
using System.Collections.Generic;

namespace AnyService
{
    public class WorkContext
    {
        public Type CurrentType => CurrentEntityConfigRecord?.Type;
        public EntityConfigRecord CurrentEntityConfigRecord { get; set; }
        public string CurrentUserId { get; set; }
        public RequestInfo RequestInfo { get; set; }

        /// <summary>
        /// Placeholder for all workcontext items that do nt have dedicated property
        /// </summary>
        /// <value></value>
        public IReadOnlyDictionary<string, object> Parameters { get; set; }
    }
}