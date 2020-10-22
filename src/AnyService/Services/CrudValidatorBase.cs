using System;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public abstract class CrudValidatorBase<TEntity>
    {
        private Type _type;
        public Type Type => _type ??= typeof(TEntity);
        public abstract Task<bool> ValidateForCreate(TEntity model, ServiceResponse<TEntity> serviceResponse);
        public abstract Task<bool> ValidateForGet(string id, ServiceResponse<TEntity> serviceResponse);
        public abstract Task<bool> ValidateForGet(Pagination<TEntity> pagination, ServiceResponse<Pagination<TEntity>> serviceResponse);
        public abstract Task<bool> ValidateForUpdate(TEntity model, ServiceResponse<TEntity> serviceResponse);
        public abstract Task<bool> ValidateForDelete(string id, ServiceResponse<TEntity> serviceResponse);
    }
}
