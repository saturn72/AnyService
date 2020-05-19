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

        public virtual void PrepareForUpdate(IUpdatableAudit before, IUpdatableAudit after, string userId)
        {
            if (after is ICreatableAudit)
            {
                var c = after as ICreatableAudit;
                var dc = before as ICreatableAudit;
                c.CreatedByUserId = dc.CreatedByUserId;
                c.CreatedOnUtc = dc.CreatedOnUtc;
            }

            var updateRecords = before.UpdateRecords?.ToList() ?? new List<UpdateRecord>();
            updateRecords.Add(new UpdateRecord { UpdatedOnUtc = DateTime.UtcNow.ToIso8601(), UpdatedByUserId = userId });
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