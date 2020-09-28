using System.Threading.Tasks;

namespace AnyService.Services.Preparars
{
    public interface IModelPreparar<TDomainModel> where TDomainModel : IDomainObject
    {
        Task PrepareForCreate(TDomainModel model);
        Task PrepareForUpdate(TDomainModel beforeModel, TDomainModel afterModel);
        Task PrepareForDelete(TDomainModel model);
    }
}