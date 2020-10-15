using System.Collections.Generic;

namespace AnyService.Services
{
    public class EntityMappingResponse
    {
        public EntityMappingResponse(EntityMappingRequest request)
        {
            Request = request;
        }
        public EntityMappingRequest Request { get; }
        public IEnumerable<EntityMapping> EntityMappingsAdded { get; set; }
        public IEnumerable<EntityMapping> EntityMappingsRemoved { get; set; }
    }
}
