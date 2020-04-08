using System;
using System.Collections.Generic;

namespace AnyService.Utilities
{
    public class IdGeneratorFactory
    {
        private readonly IDictionary<Type, IIdGenerator> _generators = new Dictionary<Type, IIdGenerator>();
        public void AddOrReplace(Type type, IIdGenerator generator) => _generators[type] = generator;

        public virtual IIdGenerator GetGenerator(Type type)
        {
            _generators.TryGetValue(type, out IIdGenerator value);
            return value;
        }
        public object GetNext(Type type) => _generators[type].GetNext();
    }
}