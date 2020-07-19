using System;
using System.Collections.Generic;

namespace AnyService
{
    public class WorkContext
    {
        public Type CurrentType => CurrentEntityConfigRecord?.Type;
        public EntityConfigRecord CurrentEntityConfigRecord { get; set; }
        public string CurrentUserId
        {
            get => GetParameterOrDefault<string>(nameof(CurrentUserId));
            set => SetParameter(nameof(CurrentUserId), value);
        }
        public string CurrentClientId
        {
            get => GetParameterOrDefault<string>(nameof(CurrentClientId));
            set => SetParameter(nameof(CurrentClientId), value);
        }
        public RequestInfo RequestInfo { get; set; }
        public string IpAddress
        {
            get => GetParameterOrDefault<string>(nameof(IpAddress));
            set => SetParameter(nameof(IpAddress), value);
        }

        /// <summary>
        /// Placeholder for all workcontext items that do nt have dedicated property
        /// </summary>
        /// <value></value>
        public IReadOnlyDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        protected virtual void SetParameter(string key, object value) => (Parameters as Dictionary<string, object>)[key] = value;
        protected virtual T GetParameterOrDefault<T>(string key) => Parameters.TryGetValue(key, out object value) ? (T)value : default;
    }
}