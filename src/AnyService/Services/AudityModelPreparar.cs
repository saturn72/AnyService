using AnyService.Audity;
using AnyService.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public class AudityModelPreparar<TDomainModel> : IModelPreparar<TDomainModel> where TDomainModel : IDomainModelBase
    {
        private static readonly Type CreatableType = typeof(ICreatableAudit);
        private static readonly Type UpdateableType = typeof(IUpdatableAudit);
        private static readonly Type DeletableType = typeof(IDeletableAudit);
        protected readonly AuditHelper AuditHelper;
        protected readonly WorkContext WorkContext;
        protected readonly ILogger<AudityModelPreparar<TDomainModel>> Logger;
        protected IReadOnlyDictionary<Type, IDictionary<Type, bool>> IsOfTypeCollection { get; set; }

        public AudityModelPreparar(AuditHelper auditHelper, WorkContext workContext, ILogger<AudityModelPreparar<TDomainModel>> logger)
        {
            AuditHelper = auditHelper;
            WorkContext = workContext;
            Logger = logger;
            IsOfTypeCollection = new Dictionary<Type, IDictionary<Type, bool>>
            {
                {CreatableType, new Dictionary<Type, bool>()},
                {UpdateableType, new Dictionary<Type, bool>()},
                {DeletableType, new Dictionary<Type, bool>()},
            };
        }
        public virtual Task PrepareForCreate(TDomainModel model)
        {
            if (IsOfType(CreatableType, typeof(TDomainModel)))
            {
                Logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for creation");
                AuditHelper.PrepareForCreate(model as ICreatableAudit, WorkContext.CurrentUserId);
            }
            return Task.CompletedTask;
        }
        public virtual Task PrepareForUpdate(TDomainModel beforeModel, TDomainModel afterModel)
        {
            if (IsOfType(UpdateableType, typeof(TDomainModel)))
            {
                Logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for update");
                AuditHelper.PrepareForUpdate(beforeModel as IUpdatableAudit, afterModel as IUpdatableAudit, WorkContext.CurrentUserId);
            }
            return Task.CompletedTask;
        }
        public virtual Task PrepareForDelete(TDomainModel model)
        {
            if (IsOfType(DeletableType, typeof(TDomainModel)))
                AuditHelper.PrepareForDelete(model as IDeletableAudit, WorkContext.CurrentUserId);
            return Task.CompletedTask;
        }
        #region Utilities
        protected bool IsOfType(Type key, Type type)
        {
            var col = IsOfTypeCollection[key];

            if (!col.TryGetValue(type, out bool value))
                value = col[type] = key.IsAssignableFrom(type);
            return value;
        }
        #endregion`
    }
}