using System;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface ICrudValidator
    {
        Type Type { get; }
    }
    public abstract class CrudValidatorBase<TDomainModel> : ICrudValidator
    {
        private Type _type;
        public Type Type => _type ??= typeof(TDomainModel);
        public abstract Task<bool> ValidateForCreate(TDomainModel model, ServiceResponse serviceResponse);
        public abstract Task<bool> ValidateForGet(ServiceResponse serviceResponse);
        public abstract Task<bool> ValidateForUpdate(TDomainModel model, ServiceResponse serviceResponse);
        public abstract Task<bool> ValidateForDelete(string id, ServiceResponse serviceResponse);
    }
}
