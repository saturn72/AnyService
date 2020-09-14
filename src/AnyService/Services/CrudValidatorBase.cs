using System;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public abstract class CrudValidatorBase<TDomainModel>
    {
        private Type _type;
        public Type Type => _type ??= typeof(TDomainModel);
        public abstract Task<bool> ValidateForCreate(TDomainModel model, ServiceResponse<TDomainModel> serviceResponse);
        public abstract Task<bool> ValidateForGet(string id, ServiceResponse<TDomainModel> serviceResponse);
        public abstract Task<bool> ValidateForGet(ServiceResponse<Pagination<TDomainModel>> serviceResponse);
        public abstract Task<bool> ValidateForUpdate(TDomainModel model, ServiceResponse<TDomainModel> serviceResponse);
        public abstract Task<bool> ValidateForDelete(string id, ServiceResponse<TDomainModel> serviceResponse);
    }
}
