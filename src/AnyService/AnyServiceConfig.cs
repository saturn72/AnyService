using System;
using System.Collections.Generic;
using AnyService.Core;
using AnyService.Services;

namespace AnyService
{
    public sealed partial class AnyServiceConfig
    {
        public AnyServiceConfig()
        {
            MaxMultipartBoundaryLength = 50;
            MaxValueCount = 25;
            ManageEntityPermissions = true;
            DefaultPaginationSettings = new PaginationSettings
            {
                DefaultOrderBy = nameof(IDomainModelBase.Id),
                DefaultOffset = 0,
                DefaultPageSize = 50,
                DefaultSortOrder = PaginationSettings.Asc,
            };
            FilterFactoryType = typeof(DefaultFilterFactory);
            ModelPrepararType = typeof(DefaultModelPreparar<>);
        }

        public IEnumerable<EntityConfigRecord> EntityConfigRecords { get; set; }

        /// <summary>
        /// Gets or sets value indicating whether anyservice manages user permission on entity. 
        /// default value = true
        /// </summary>
        /// <value></value>
        public bool ManageEntityPermissions { get; set; }
        public int MaxMultipartBoundaryLength { get; set; }
        public int MaxValueCount { get; set; }
        public PaginationSettings DefaultPaginationSettings { get; set; }
        /// <summary>
        /// type of filter factory old reserved queries for GetAll operation
        /// MUST inherit from IFilterFactory
        /// </summary>
        /// <value></value>
        public Type FilterFactoryType { get; set; }
        public Type ModelPrepararType { get; set; }
    }
}