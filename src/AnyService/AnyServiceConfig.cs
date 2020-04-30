using System;
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

        public IReadOnlyDictionary<string, Func<object, Func<object, string>>> GetAllQueries { get; set; }

        #region nested classes
        private class DefaultGetAllQueries
        {
            internal DefaultGetAllQueries()
            {
                Queries = new Dictionary<string, Func<object, Func<object, string>>>
                {
                    {"__created", _createdByUser },
                    // {"__updated", UpdatedByUser },
                    {"__deleted", _deletedByUser },
                    {"__public", _isPublic}
                };
            }

            private Func<object, string> _createdByUser(object payload)
            {
                var type = payload.GetPropertyValueByName<Type>("Type");
                var userId = payload.GetPropertyValueByName<Type>("UserId");
                if (type is ICreatableAudit)
                    return o => $"{nameof(ICreatableAudit.CreatedByUserId)} == {userId}";

                return null;
            }
            private Func<object, string> _deletedByUser(object payload)
            {
                var type = payload.GetPropertyValueByName<Type>("Type");
                var userId = payload.GetPropertyValueByName<Type>("UserId");
                if (type is IDeletableAudit)
                    return o => $"{nameof(IDeletableAudit.DeletedByUserId)} == {userId}";

                return null;
            }
            // private Func<object, string> _updatedByUser(object payload)
            // {
            //     var type = payload.GetPropertyValueByName<Type>("Type");
            //     var userId = payload.GetPropertyValueByName<Type>("UserId");
            //     if ((type as IUpdatableAudit) != null)
            //         return o => $"{nameof(IUpdatableAudit.UpdateRecords).Equals.CreatedByUserId)} == {userId}";

            //     return null;
            // }
            private Func<object, string> _isPublic(object payload)
            {
                var type = payload.GetPropertyValueByName<Type>("Type");
                if (type is IPublishable)
                    return o => $"{nameof(IPublishable.Public)} == true";
                return null;
            }
            internal IReadOnlyDictionary<string, Func<object, Func<object, string>>> Queries { get; private set; }
        }
        #endregion
    }
}