using System;
using System.Collections.Generic;

namespace AnyService
{
    public abstract class ExtendableBase
    {
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        public virtual void SetParameter(string key, object value) => (Parameters as Dictionary<string, object>)[key] = value;
        public virtual T GetParameterOrDefault<T>(string key) => Parameters.TryGetValue(key, out object value) ? (T)value : default;
    }
}
