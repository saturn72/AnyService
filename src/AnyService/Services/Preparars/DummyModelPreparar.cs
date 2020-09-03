using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services.Preparars
{
    public class DummyModelPreparar<TDomainModel> : IModelPreparar<TDomainModel> where TDomainModel : IDomainModelBase
    {

        protected readonly WorkContext WorkContext;
        protected readonly ILogger<DummyModelPreparar<TDomainModel>> Logger;
        protected IReadOnlyDictionary<Type, IDictionary<Type, bool>> IsOfTypeCollection { get; set; }

        public DummyModelPreparar(ILogger<DummyModelPreparar<TDomainModel>> logger)
        {
            Logger = logger;
        }
        public virtual Task PrepareForCreate(TDomainModel model)
        {
            Logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for creation. Do nothing");
            return Task.CompletedTask;
        }
        public virtual Task PrepareForUpdate(TDomainModel beforeModel, TDomainModel afterModel)
        {
            Logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for update. Do nothing");
            return Task.CompletedTask;
        }
        public virtual Task PrepareForDelete(TDomainModel model)
        {
            Logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for delete. Do nothing");
            return Task.CompletedTask;
        }
    }
}