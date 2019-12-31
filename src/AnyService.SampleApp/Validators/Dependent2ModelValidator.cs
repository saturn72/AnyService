using System;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.SampleApp.Models;

namespace AnyService.SampleApp.Validators
{
    public class Dependent2ModelValidator : ICrudValidator<Dependent2>
    {
        public Type Type => typeof(Dependent2);
        public Task<bool> ValidateForCreate(Dependent2 model, ServiceResponse serviceResponse)
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

        public Task<bool> ValidateForUpdate(Dependent2 model, ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }
    }
}