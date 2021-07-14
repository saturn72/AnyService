using System;
using System.Collections.Generic;
using System.ComponentModel;
using AnyService.Audity;
using AnyService.Controllers;
using AnyService.Services;
using AnyService.Services.Preparars;
using AnyService.Services.ServiceResponseMappers;

namespace AnyService
{
    public sealed partial class AnyServiceConfig
    {
        private string _errorEventKey;
        public AnyServiceConfig()
        {
            AuditSettings = new AuditSettings
            {
                AuditRules = new AuditRules
                {
                    AuditCreate = true,
                    AuditRead = true,
                    AuditUpdate = true,
                    AuditDelete = true,
                }
            };
            DefaultPaginationSettings = new PaginationSettings();
            ErrorEventKey = LoggingEvents.UnexpectedSystemExceptionName;
        }
        /// <summary>
        /// Gets or sets the sudit config for the entity
        /// </summary>
        public AuditSettings AuditSettings { get; set; }
        public PaginationSettings DefaultPaginationSettings { get; set; }
        public IEnumerable<EntityConfigRecord> EntityConfigRecords { get; set; }

        [DefaultValue(LoggingEvents.UnexpectedSystemExceptionName)]
        public string ErrorEventKey
        {
            get => _errorEventKey;
            set
            {
                _errorEventKey = value;
                ErrorController.ErrorEventKey = _errorEventKey;
            }
        }
        /// <summary>
        /// type of filter factory old reserved queries for GetAll operation
        /// MUST inherit from IFilterFactory
        /// </summary>
        /// <value></value>
        [DefaultValue(typeof(DefaultFilterFactory))]
        public Type FilterFactoryType { get; set; } = typeof(DefaultFilterFactory);
        /// <summary>
        /// Gets or sets value indicating whether anyservice manages user permission on entity. 
        /// default value = true
        /// </summary>
        /// <value></value>
        public bool ManageEntityPermissions { get; set; } = true;
        /// <summary>
        /// sets the default mapper name
        /// </summary>
        [DefaultValue("default")]
        public string MapperName { get; set; } = "default";
        [DefaultValue(50)]
        public int MaxMultipartBoundaryLength { get; set; } = 50;
        [DefaultValue(25)]
        public int MaxValueCount { get; set; } = 25;
        [DefaultValue(typeof(DummyModelPreparar<>))]
        public Type ModelPrepararType { get; set; } = typeof(DummyModelPreparar<>);
        [DefaultValue(typeof(DataOnlyServiceResponseMapper))]
        public Type ServiceResponseMapperType { get; set; } = typeof(DataOnlyServiceResponseMapper);
        [DefaultValue(false)]
        public bool OutputErrorOnNonDevelopementEnv { get; set; }
        [DefaultValue(true)]
        public bool UseLogRecordEndpoint { get; set; } = true;
    }
}