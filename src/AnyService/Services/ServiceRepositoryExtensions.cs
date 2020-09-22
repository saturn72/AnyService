using System;
using System.Collections;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public static class ServiceRepositoryExtensions
    {
        public static async Task<TResult> Command<TDomainModel, TResult>(
            this IRepository<TDomainModel> repository,
            Func<IRepository<TDomainModel>, Task<TResult>> command,
            ServiceResponseWrapper wrapper)
                where TDomainModel : IDomainModelBase
        {
            var data = default(TResult);
            var serviceResponse = wrapper.ServiceResponse;
            try
            {
                data = await command(repository);
            }
            catch (Exception ex)
            {
                serviceResponse.Result = ServiceResult.Error;
                serviceResponse.Message = "Unlnown error while command repository";
                wrapper.Exception = ex;
                return data;
            }

            if (data == null)
            {
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                serviceResponse.Message = "Failed to command repository";
                return data;
            }
            serviceResponse.PayloadObject = data;

            return data;
        }
        public static async Task<TResult> Query<TDomainModel, TResult>(
            this IRepository<TDomainModel> repository,
            Func<IRepository<TDomainModel>, Task<TResult>> command,
            ServiceResponseWrapper wrapper)
            where TDomainModel : IDomainModelBase
        {
            var data = default(TResult);
            var serviceResponse = wrapper.ServiceResponse;
            try
            {
                data = await command(repository);
            }
            catch (Exception ex)
            {
                wrapper.Exception = ex;
                serviceResponse.Result = ServiceResult.Error;
                serviceResponse.Message = "Unlnown error command repository:\n\t" + ex.Message;
                return data;
            }

            if (data == null && !typeof(IEnumerable).IsAssignableFrom(typeof(TResult)))
            {
                serviceResponse.Result = ServiceResult.NotFound;
                serviceResponse.Message = "Item not found in repository";
            }

            serviceResponse.PayloadObject = data;
            return data;
        }
    }
}