using AutoMapper;

namespace AnyService.Mapping
{
    public interface IMapperFactory
    {
        void AddMapper(string mapperName, IMapper mapper);
        IMapper GetMapper(string mapperName);
    }
}
