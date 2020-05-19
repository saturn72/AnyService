using AnyService.Audity;
using AnyService.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AnyService.Services
{
    public class DefaultModelPreparar<TDomainModel> : IModelPreparar<TDomainModel> where TDomainModel : IDomainModelBase
    {
        private readonly AuditHelper _auditHelper;
        private readonly WorkContext _workContext;
        private readonly ILogger<DefaultModelPreparar<TDomainModel>> _logger;

        public DefaultModelPreparar(AuditHelper auditHelper, WorkContext workContext, ILogger<DefaultModelPreparar<TDomainModel>> logger)
        {
            _auditHelper = auditHelper;
            _workContext = workContext;
            _logger = logger;
        }
        public Task PrepareForCreate(TDomainModel model)
        {
            if (model is ICreatableAudit)
            {
                _logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for creation");
                _auditHelper.PrepareForCreate(model as ICreatableAudit, _workContext.CurrentUserId);
            }
            return Task.CompletedTask;
        }
        public Task PrepareForUpdate(TDomainModel beforeModel, TDomainModel afterModel)
        {
            if (beforeModel is IUpdatableAudit)
            {
                _logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for update");
                _auditHelper.PrepareForUpdate(beforeModel as IUpdatableAudit, afterModel as IUpdatableAudit, _workContext.CurrentUserId);
            }
            return Task.CompletedTask;
        }
        public Task PrepareForDelete(TDomainModel model)
        {
            _auditHelper.PrepareForDelete(model as IDeletableAudit, _workContext.CurrentUserId);
            return Task.CompletedTask;
        }

    }
}