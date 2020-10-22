using System.Threading.Tasks;

namespace AnyService.Services.Preparars
{
    public interface IModelPreparar<TEntity> where TEntity : IEntity
    {
        Task PrepareForCreate(TEntity model);
        Task PrepareForUpdate(TEntity beforeModel, TEntity afterModel);
        Task PrepareForDelete(TEntity model);
    }
}