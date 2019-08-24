using System;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface ICrudValidator
    {
        Type Type { get; }
    }
    public interface ICrudValidator<TDomainModel> : ICrudValidator
    {
        Task<bool> ValidateForCreate(TDomainModel model, ServiceResponse serviceResponse);
        Task<bool> ValidateForGet(ServiceResponse serviceResponse);
        Task<bool> ValidateForUpdate(TDomainModel model, ServiceResponse serviceResponse);
        Task<bool> ValidateForDelete(string id, ServiceResponse serviceResponse);
    }
}
