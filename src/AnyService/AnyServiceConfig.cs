using System;
using System.Collections.Generic;
using AnyService.Audity;
using AnyService.Services;
using AnyService.Services.Preparars;
using AnyService.Services.ServiceResponseMappers;
using AvosetGoOnline.Controllers;

namespace AnyService
{
    public sealed partial class AnyServiceConfig
    {
        private string _errorEventKey;
        public AnyServiceConfig()
        {
            MaxMultipartBoundaryLength = 50;
            ErrorEventKey = LoggingEvents.UnexpectedException.Name;
            MaxValueCount = 25;
            DefaultPaginationSettings = new PaginationSettings
            {
                DefaultOrderBy = nameof(IDomainModelBase.Id),
                DefaultOffset = 0,
                DefaultPageSize = 50,
                DefaultSortOrder = PaginationSettings.Asc,
            };
            FilterFactoryType = typeof(DefaultFilterFactory);
            ModelPrepararType = typeof(DummyModelPreparar<>);
            ServiceResponseMapperType = typeof(DataOnlyServiceResponseMapper);
            AuditSettings = new AuditSettings
            {
                Active = true,
                AuditRules = new AuditRules
                {
                    AuditCreate = true,
                    AuditRead = true,
                    AuditUpdate = true,
                    AuditDelete = true,
                }
            };
        }

        public IEnumerable<EntityConfigRecord> EntityConfigRecords { get; set; }

        /// <summary>
        /// Gets or sets value indicating whether anyservice manages user permission on entity. 
        /// default value = true
        /// </summary>
        /// <value></value>
        public bool ManageEntityPermissions { get; set; } = true;
        public int MaxMultipartBoundaryLength { get; set; }
        public string ErrorEventKey
        {
            get => _errorEventKey;
            set
            {
                _errorEventKey = value;
                ErrorController.ErrorEventKey = _errorEventKey;
            }
        }
        public int MaxValueCount { get; set; }
        public PaginationSettings DefaultPaginationSettings { get; set; }
        /// <summary>
        /// type of filter factory old reserved queries for GetAll operation
        /// MUST inherit from IFilterFactory
        /// </summary>
        /// <value></value>
        public Type FilterFactoryType { get; set; }
        public Type ModelPrepararType { get; set; }
        public Type ServiceResponseMapperType { get; set; }
        /// <summary>
        /// Gets or sets the sudit config for the entity
        /// </summary>
        public AuditSettings AuditSettings { get; set; }
    }
}