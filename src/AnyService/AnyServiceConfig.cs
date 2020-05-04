using System;
using System.Linq;
using System.Collections.Generic;
using AnyService.Audity;
using AnyService.Core;
using AnyService.Services;

namespace AnyService
{
    public sealed class AnyServiceConfig
    {
        public AnyServiceConfig()
        {
            MaxMultipartBoundaryLength = 50;
            MaxValueCount = 25;
            ManageEntityPermissions = true;
            UseAuthorizationMiddleware = true;
            UseExceptionLogging = true;
            DefaultPaginationSettings = new PaginationSettings
            {
                DefaultOrderBy = nameof(IDomainModelBase.Id),
                DefaultOffset = 1,
                DefaultPageSize = 50,
                DefaultSortOrder = PaginationSettings.Asc,
            };
            GetAllQueries = new DefaultGetAllQueries().Queries;
        }

        public IEnumerable<EntityConfigRecord> EntityConfigRecords { get; set; }

        /// <summary>
        /// Gets or sets value indicating whether anyservice manages user permission on entity. 
        /// default value = true
        /// </summary>
        /// <value></value>
        public bool ManageEntityPermissions { get; set; }
        public bool UseAuthorizationMiddleware { get; set; }
        public bool UseExceptionLogging { get; set; }
        public int MaxMultipartBoundaryLength { get; set; }
        public int MaxValueCount { get; set; }
        public PaginationSettings DefaultPaginationSettings { get; set; }

        public IReadOnlyDictionary<string, Func<object, Func<object, bool>>> GetAllQueries { get; set; }

        #region nested classes
        private class DefaultGetAllQueries
        {
            internal IReadOnlyDictionary<string, Func<object, Func<object, bool>>> Queries { get; private set; }
            internal DefaultGetAllQueries()
            {
                var rq = new Dictionary<string, Func<object, Func<object, bool>>>()
                {
                    {"__created",  _createdByUser()},
                    {"__updated", _updatedByUser()},
                    {"__deleted", _deletedByUser()},
                    {"__public", _isPublic()},
                };
                Queries = rq;
            }

            private Func<object, Func<object, bool>> _createdByUser()
            {
                return payload =>
                {
                    var type = payload.GetPropertyValueByName<Type>("Type");
                    var userId = payload.GetPropertyValueByName<string>("UserId");
                    return type is ICreatableAudit ?
                        x => (x as ICreatableAudit).CreatedByUserId == userId :
                        null as Func<object, bool>;
                };
            }
            private Func<object, Func<object, bool>> _deletedByUser()
            {
                return payload =>
               {
                   var type = payload.GetPropertyValueByName<Type>("Type");
                   var userId = payload.GetPropertyValueByName<string>("UserId");
                   return type is IDeletableAudit ?
                       x => (x as IDeletableAudit).DeletedByUserId == userId :
                       null as Func<object, bool>;
               };
            }
            private Func<object, Func<object, bool>> _updatedByUser()
            {
                return payload =>
                {
                    var type = payload.GetPropertyValueByName<Type>("Type");
                    var userId = payload.GetPropertyValueByName<string>("UserId");
                    return type is IUpdatableAudit ? x => (x as IUpdatableAudit).UpdateRecords.Any(ur => ur.UpdatedByUserId == userId) :
                        null as Func<object, bool>;
                };
            }
            private Func<object, Func<object, bool>> _isPublic()
            {
                return payload =>
                {
                    var type = payload.GetPropertyValueByName<Type>("Type");
                    return type is IPublishable ? x => (x as IPublishable).Public :
                        null as Func<object, bool>;
                };
            }
        }
        #endregion
    }
}