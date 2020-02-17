using AnyService.Services;
using AnyService.Core;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AnyService.EntityFramework
{
    public class EfRepository<TDomainModel> : IRepository<TDomainModel> where TDomainModel : IDomainModelBase
    {
        public Task<IEnumerable<TDomainModel>> GetAll(IDictionary<string, string> filter = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<TDomainModel> GetById(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<TDomainModel> Insert(TDomainModel entity)
        {
            throw new System.NotImplementedException();
        }

        public Task<TDomainModel> Update(TDomainModel entity)
        {
            throw new System.NotImplementedException();
        }
    }
}