using System;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.SampleApp.Models;

namespace AnyService.SampleApp.Validators
{
    public class DependentModelValidator : ICrudValidator<DependentModel>
    {
        public Type Type => typeof(DependentModel);
        public Task<bool> ValidateForCreate(DependentModel model, ServiceResponse serviceResponse)
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

        public Task<bool> ValidateForUpdate(DependentModel model, ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }
    }
}