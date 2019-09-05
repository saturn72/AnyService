using System;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.SampleApp.Models;

namespace AnyService.SampleApp.Validators
{
    public class MultipartSampleValidator : ICrudValidator<MultipartSampleModel>
    {
        public Type Type => typeof(MultipartSampleModel);

        public Task<bool> ValidateForCreate(MultipartSampleModel model, ServiceResponse serviceResponse)
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

        public Task<bool> ValidateForUpdate(MultipartSampleModel model, ServiceResponse serviceResponse)
        {
            return Task.FromResult(true);
        }
    }
}