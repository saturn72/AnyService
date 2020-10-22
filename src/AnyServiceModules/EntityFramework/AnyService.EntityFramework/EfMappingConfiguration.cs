using System;
using System.Collections.Generic;

namespace AnyService.EntityFramework
{
    public sealed class EfMappingConfiguration
    {
        public string MapperName { get; set; } = "ef-mapping-repository-mapper";
        public IReadOnlyDictionary<Type, Type> EntitiesToDbModelsMaps { get; set; }
    }
}
