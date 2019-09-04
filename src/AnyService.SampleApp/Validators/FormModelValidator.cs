using System;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.SampleApp.Models;

namespace AnyService.SampleApp.Validators
{
    public class FormModelValidator : ICrudValidator<FormModel>
    {
        public Type Type => typeof(FormModel);

        public Task<bool> ValidateForCreate(FormModel model, ServiceResponse serviceResponse)
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

        public Task<bool> ValidateForUpdate(FormModel model, ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }
    }
}