using System.Collections.Generic;
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
    }
}