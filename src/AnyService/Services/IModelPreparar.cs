using AnyService.Core;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public interface IModelPreparar<TDomainModel> where TDomainModel : IDomainModelBase
    {
        Task PrepareForCreate(TDomainModel model);
        Task PrepareForUpdate(TDomainModel beforeModel, TDomainModel afterModel);
        Task PrepareForDelete(TDomainModel model);
    }
}