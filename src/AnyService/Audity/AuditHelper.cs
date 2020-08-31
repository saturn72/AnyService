using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace AnyService.Audity
{
    public class AuditHelper
    {
        private readonly IServiceProvider _serviceProvider;

        public AuditHelper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public virtual void PrepareForCreate(ICreatableAudit creatable, string userId)
        {
            var createdOnUtc = DateTime.UtcNow.ToIso8601();
            creatable.CreatedOnUtc = createdOnUtc;
            creatable.CreatedByUserId = userId;
            creatable.CreatedWorkContextJson = _serviceProvider.GetService<WorkContext>().Parameters.ToJsonString();
        }

        public virtual void PrepareForUpdate(IUpdatableAudit before, IUpdatableAudit after, string userId)
        {
            if (after is ICreatableAudit)
            {
                var a = after as ICreatableAudit;
                var b = before as ICreatableAudit;
                a.CreatedByUserId = b.CreatedByUserId;
                a.CreatedOnUtc = b.CreatedOnUtc;
                a.CreatedWorkContextJson = b.CreatedWorkContextJson;
            }

            var updateRecords = before.UpdateRecords?.ToList() ?? new List<UpdateRecord>();
            var uRecord = new UpdateRecord
            {
                UpdatedOnUtc = DateTime.UtcNow.ToIso8601(),
                UpdatedByUserId = userId,
                WorkContextJson = _serviceProvider.GetService<WorkContext>().Parameters.ToJsonString()
            };
            updateRecords.Add(uRecord);
            after.UpdateRecords = updateRecords;
        }

        public virtual void PrepareForDelete(IDeletableAudit deletable, string userId)
        {
            var deletedOnUtc = DateTime.UtcNow.ToIso8601();
            deletable.DeletedOnUtc = deletedOnUtc;
            deletable.DeletedByUserId = userId;
            deletable.Deleted = true;
        }
    }
}