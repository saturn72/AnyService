using System.Collections.Generic;

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
            DefaultPaginateSettings = new PaginateSettings
            {
                DefaultOffset = 1,
                DefaultPageSize = 50,
                DefaultSortOrder = PaginateSettings.Asc,
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
        public int MaxMultipartBoundaryLength { get; set; }
        public int MaxValueCount { get; set; }
        public PaginateSettings DefaultPaginateSettings { get; set; }
    }
}