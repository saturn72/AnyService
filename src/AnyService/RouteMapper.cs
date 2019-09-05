using System;
using System.Linq;
using System.Collections.Generic;

namespace AnyService
{
    public class RouteMapper
    {
        internal RouteMapper(IReadOnlyDictionary<Type, TypeConfigRecord> maps)
        {
            var m = maps.ToDictionary(kvp => kvp.Value.RoutePrefix.ToLower(), kvp => kvp.Value.Type);
            Maps = new Dictionary<string, Type>(m, StringComparer.InvariantCultureIgnoreCase);
        }
        public IReadOnlyDictionary<string, Type> Maps { get; }
    }
}
