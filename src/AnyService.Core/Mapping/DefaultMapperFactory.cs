using AutoMapper;
using System.Collections.Concurrent;

namespace AnyService.Mapping
{
    public class DefaultMapperFactory : IMapperFactory
    {
        private readonly ConcurrentDictionary<string, IMapper> _mappers;
        public DefaultMapperFactory()
        {
            _mappers = new ConcurrentDictionary<string, IMapper>();
        }
        public void AddMapper(string mapperName, IMapper mapper) => _mappers[mapperName] = mapper;
        public IMapper GetMapper(string mapperName)
        {
            _mappers.TryGetValue(mapperName, out IMapper mapper);
            return mapper;
        }
    }
}
