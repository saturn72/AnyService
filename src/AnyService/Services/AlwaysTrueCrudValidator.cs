
using System;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public class AlwaysTrueCrudValidator<TDomainModel> : ICrudValidator<TDomainModel>
    {
        public Type Type => typeof(TDomainModel);
        public Task<bool> ValidateForCreate(TDomainModel model, ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ValidateForDelete(string id, ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ValidateForGet(ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ValidateForUpdate(TDomainModel model, ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }
    }
}
