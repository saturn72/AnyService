using System.Threading.Tasks;

namespace AnyService.Services
{
    public class AlwaysTrueCrudValidator<TEntity> : CrudValidatorBase<TEntity> where TEntity : IEntity
    {
        public override Task<bool> ValidateForCreate(TEntity model, ServiceResponse<TEntity> serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForDelete(string id, ServiceResponse<TEntity> serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForGet(string id, ServiceResponse<TEntity> serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForGet(Pagination<TEntity> pagination, ServiceResponse<Pagination<TEntity>> serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForUpdate(TEntity model, ServiceResponse<TEntity> serviceResponse) => Task.FromResult(true);
    }
}
