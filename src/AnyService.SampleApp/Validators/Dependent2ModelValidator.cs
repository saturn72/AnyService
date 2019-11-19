using System;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.SampleApp.Models;

namespace AnyService.SampleApp.Validators
{
    public class Dependent2ModelValidator : ICrudValidator<Dependent2Model>
    {
        public Type Type => typeof(Dependent2Model);
        public Task<bool> ValidateForCreate(Dependent2Model model, ServiceResponse serviceResponse)
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

        public Task<bool> ValidateForUpdate(Dependent2Model model, ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }
    }
}