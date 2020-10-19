using AutoMapper;

namespace AnyService
{
    public interface IMapperFactory
    {
        void AddMapper(string mapperName, IMapper mapper);
        IMapper GetMapper(string mapperName);
    }
}
