using System;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using AnyService.Audity;
using System.Collections.Generic;

namespace AnyService.Services
{
    public static class ServiceRepositoryExtensions
    {
        public static async Task<TDomainModel> Command<TDomainModel>(
            this IRepository<TDomainModel> repository,
            Func<IRepository<TDomainModel>, Task<TDomainModel>> command,
            ServiceResponse serviceResponse) where TDomainModel : IDomainModelBase
        {
            var data = default(TDomainModel);
            try
            {
                data = await command(repository);
            }
            catch (Exception ex)
            {
                serviceResponse.Result = ServiceResult.Error;
                serviceResponse.Message = "Unlnown error command repository:\n\t" + ex.Message;
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
        public static async Task<TResult> Query<TDomainModel, TResult>(this IRepository<TDomainModel> repository, Func<IRepository<TDomainModel>, Task<TResult>> command, ServiceResponse serviceResponse)
            where TDomainModel : IDomainModelBase
        {
            var data = default(TResult);
            try
            {
                data = await command(repository);
            }
            catch (Exception ex)
            {
                serviceResponse.Result = ServiceResult.Error;
                serviceResponse.Message = "Unlnown error command repository:\n\t" + ex.Message;
                return data;
            }

            var isEnumerable = data is IEnumerable;
            if (data == null
                || (data is IDeletableAudit && (data as IDeletableAudit).Deleted)
                || (isEnumerable && (data as IEnumerable).IsNullOrEmpty()))
            {
                serviceResponse.Result = ServiceResult.NotFound;
                serviceResponse.Message = "Item not found in repository";
            }

            var temp = new List<object>();
            if (isEnumerable)
            {
                foreach (var item in data as IEnumerable)
                {
                    if (item is IDeletableAudit && (item as IDeletableAudit).Deleted)
                        continue;
                    temp.Add(item);
                }
            }

            var finalData = isEnumerable ? temp : data as object;
            serviceResponse.Data = finalData;
            return (TResult)finalData;
        }
    }
}