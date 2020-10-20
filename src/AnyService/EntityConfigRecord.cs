using System;
using System.Collections.Generic;
using AnyService.Audity;
using AnyService.Security;
using AnyService.Services;
using AnyService.Services.Internals;

namespace AnyService
{
    public class EntityConfigRecord
    {
        private Type _type;

        /// <summary>
        /// Gets or sets entity name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 
        /// Gets or sets entity type 
        /// </summary>
        public Type Type
        {
            get => _type;
            set
            {
                _type = value;
                Metadata = new DomainEntityMetadata(_type);
            }
        }
        /// <summary>
        /// Gets or sets event keys for the entity. 
        /// These keys are used to uniquly identify CRUD operation events on the entity. 
        /// </summary>
        public EventKeyRecord EventKeys { get; set; }
        /// <summary>
        /// Gets or sets entity permission record keys
        /// These keys uniqly identify entity's permission record keys, which used durin entity authorization.
        /// </summary>
        public PermissionRecord PermissionRecord { get; set; }
        /// <summary>
        /// Gets or sets unique identifier for this entity.
        /// </summary>
        public string EntityKey { get; set; }
        /// <summary>
        /// Gets or sets collection contains endpoint behavior
        /// </summary>
        public IEnumerable<EndpointSettings> EndpointSettings { get; set; }
        public Type CrudValidatorType { get; set; }
        /// <summary>
        /// Gets or sets paginate default settings
        /// </summary>
        public PaginationSettings PaginationSettings { get; set; }
        public Type FilterFactoryType { get; set; }
        public Type ModelPrepararType { get; set; }
        /// <summary>
        /// Gets or sets the audit rules for the entity
        /// </summary>
        public AuditRules AuditRules { get; set; }
        internal AuditSettings AuditSettings { get; set; }
        internal DomainEntityMetadata Metadata { get; private set; }
    }
}