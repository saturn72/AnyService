using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services.Preparars
{
    public class DummyModelPreparar<TEntity> : IModelPreparar<TEntity> where TEntity : IEntity
    {
        protected readonly ILogger<DummyModelPreparar<TEntity>> Logger;
        protected IReadOnlyDictionary<Type, IDictionary<Type, bool>> IsOfTypeCollection { get; set; }

        public DummyModelPreparar(ILogger<DummyModelPreparar<TEntity>> logger)
        {
            Logger = logger;
        }
        public virtual Task PrepareForCreate(TEntity model)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Prepare for creation. Do nothing");
            return Task.CompletedTask;
        }
        public virtual Task PrepareForUpdate(TEntity beforeModel, TEntity afterModel)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Prepare for update. Do nothing");
            return Task.CompletedTask;
        }
        public virtual Task PrepareForDelete(TEntity model)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Prepare for delete. Do nothing");
            return Task.CompletedTask;
        }
    }
}