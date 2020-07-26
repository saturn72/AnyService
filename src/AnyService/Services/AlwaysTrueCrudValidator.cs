using System.Threading.Tasks;

namespace AnyService.Services
{
    public class AlwaysTrueCrudValidator<TDomainModel> : CrudValidatorBase<TDomainModel>
    {
        public override Task<bool> ValidateForCreate(TDomainModel model, ServiceResponse serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForDelete(string id, ServiceResponse serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForGet(ServiceResponse serviceResponse) => Task.FromResult(true);
        public override Task<bool> ValidateForUpdate(TDomainModel model, ServiceResponse serviceResponse) => Task.FromResult(true);
    }
}
