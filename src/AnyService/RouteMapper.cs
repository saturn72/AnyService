using System.Collections.Generic;

namespace AnyService
{
    public class RouteMapper
    {
        internal RouteMapper(IEnumerable<TypeConfigRecord> maps)
        {
            Maps = maps;
        }
        public IEnumerable<TypeConfigRecord> Maps { get; }
    }
}
