using System.Threading.Tasks;

namespace AnyService.Services
{
    public class AlwaysTrueCrudValidator<TDomainModel> : CrudValidatorBase<TDomainModel>
    {
        public override Task<bool> ValidateForCreate(TDomainModel model, ServiceResponse<TDomainModel> serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForDelete(string id, ServiceResponse<TDomainModel> serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForGet(string id, ServiceResponse<TDomainModel> serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForGet(ServiceResponse<Pagination<TDomainModel>> serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForUpdate(TDomainModel model, ServiceResponse<TDomainModel> serviceResponse) => Task.FromResult(true);
    }
}
