using System;
using System.Collections;
using System.Threading.Tasks;
using AnyService.Core;

namespace AnyService.Services
{
    public static class ServiceRepositoryExtensions
    {
        public static async Task<TDomainModel> Command<TDomainModel>(
            this IRepository<TDomainModel> repository,
            Func<IRepository<TDomainModel>, Task<TDomainModel>> command,
            ServiceResponse serviceResponse, Exception exception = null) where TDomainModel : IDomainModelBase
        {
            var data = default(TDomainModel);
            try
            {
                data = await command(repository);
            }
            catch (Exception ex)
            {
                serviceResponse.Result = ServiceResult.Error;
                serviceResponse.Message = "Unlnown error while command repository";
                exception = ex;
                return data;
            }

            if (data == null)
            {
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                serviceResponse.Message = "Failed to command repository";
                return data;
            }
            serviceResponse.Data = data;

            return data;
        }
        public static async Task<TResult> Query<TDomainModel, TResult>(
            this IRepository<TDomainModel> repository,
            Func<IRepository<TDomainModel>, Task<TResult>> command,
            ServiceResponse serviceResponse, Exception exception)
            where TDomainModel : IDomainModelBase
        {
            var data = default(TResult);
            try
            {
                data = await command(repository);
            }
            catch (Exception ex)
            {
                exception = ex;
                serviceResponse.Result = ServiceResult.Error;
                serviceResponse.Message = "Unlnown error command repository:\n\t" + ex.Message;
                return data;
            }

            if (data == null && !typeof(IEnumerable).IsAssignableFrom(typeof(TResult)))
            {
                serviceResponse.Result = ServiceResult.NotFound;
                serviceResponse.Message = "Item not found in repository";
            }

            serviceResponse.Data = data;
            return data;
        }
    }
}