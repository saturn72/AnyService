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
        private static IReadOnlyDictionary<Type, IDictionary<Type, bool>> _isOfTypeCollection;
        private IReadOnlyDictionary<Type, IDictionary<Type, bool>> IsOfTypeCollection =>
            _isOfTypeCollection ?? (_isOfTypeCollection =
            new Dictionary<Type, IDictionary<Type, bool>>{
                {CreatableType, new Dictionary<Type, bool>()},
                {UpdateableType, new Dictionary<Type, bool>()},
                {DeletableType, new Dictionary<Type, bool>()},
            });
        private readonly AuditHelper _auditHelper;
        private readonly WorkContext _workContext;
        private readonly ILogger<AudityModelPreparar<TDomainModel>> _logger;

        public AudityModelPreparar(AuditHelper auditHelper, WorkContext workContext, ILogger<AudityModelPreparar<TDomainModel>> logger)
        {
            _auditHelper = auditHelper;
            _workContext = workContext;
            _logger = logger;
        }
        public Task PrepareForCreate(TDomainModel model)
        {
            if (IsOfType(CreatableType, typeof(TDomainModel)))
            {
                _logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for creation");
                _auditHelper.PrepareForCreate(model as ICreatableAudit, _workContext.CurrentUserId);
            }
            return Task.CompletedTask;
        }
        public Task PrepareForUpdate(TDomainModel beforeModel, TDomainModel afterModel)
        {
            if (IsOfType(UpdateableType, typeof(TDomainModel)))
            {
                _logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for update");
                _auditHelper.PrepareForUpdate(beforeModel as IUpdatableAudit, afterModel as IUpdatableAudit, _workContext.CurrentUserId);
            }
            return Task.CompletedTask;
        }
        public Task PrepareForDelete(TDomainModel model)
        {
            if (IsOfType(DeletableType, typeof(TDomainModel)))
                _auditHelper.PrepareForDelete(model as IDeletableAudit, _workContext.CurrentUserId);
            return Task.CompletedTask;
        }
        #region Utilities
        private bool IsOfType(Type key, Type type)
        {
            var col = IsOfTypeCollection[key];

            if (!col.TryGetValue(type, out bool value))
                value = col[type] = key.IsAssignableFrom(type);
            return value;
        }
        #endregion`
    }
}