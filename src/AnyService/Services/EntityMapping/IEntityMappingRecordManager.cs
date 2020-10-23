using System.Threading.Tasks;

namespace AnyService.Services.EntityMapping
{

    public interface IEntityMappingRecordManager
    {
        Task<ServiceResponse<EntityMappingRequest>> UpdateMapping(EntityMappingRequest request);
    }
}
