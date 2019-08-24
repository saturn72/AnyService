using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyService.Audity
{
    public class AuditHelper
    {
        public virtual void PrepareForCreate(ICreatableAudit creatable, string userId)
        {
            var createdOnUtc = DateTime.UtcNow.ToIso8601();
            creatable.CreatedOnUtc = createdOnUtc;
            creatable.CreatedByUserId = userId;
        }

        public virtual void PrepareForUpdate(IUpdatableAudit updatable, IUpdatableAudit dbModel, string userId)
        {
            if (updatable is ICreatableAudit)
            {
                var c = updatable as ICreatableAudit;
                var dc = dbModel as ICreatableAudit;
                c.CreatedByUserId = dc.CreatedByUserId;
                c.CreatedOnUtc = dc.CreatedOnUtc;
            }

            var updateRecords = dbModel.UpdateRecords?.ToList() ?? new List<UpdateRecord>();
            updateRecords.Add(new UpdateRecord { UpdatedOnUtc = DateTime.UtcNow.ToIso8601(), UpdatedByUserId = userId });
            updatable.UpdateRecords = updateRecords;
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