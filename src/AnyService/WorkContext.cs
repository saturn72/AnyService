using System;
using System.Collections.Generic;

namespace AnyService
{
    public class WorkContext
    {
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        public Type CurrentType => CurrentEntityConfigRecord?.Type;
        public EntityConfigRecord CurrentEntityConfigRecord { get; set; }
        public string CurrentUserId { get; set; }
        public RequestInfo RequestInfo { get; set; }

        /// <summary>
        /// Placeholder for all workcontext items that do nt have dedicated property
        /// </summary>
        /// <value></value>
        public IReadOnlyDictionary<string, object> Parameters => _parameters;

        protected virtual void SetParameter(string key, object value) => _parameters[key] = value;
    }
}