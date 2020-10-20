using System;
using AnyService.Audity;
using AnyService.Security;
using AnyService.Services;

namespace AnyService
{
    public class EntityConfigRecord
    {
        private bool _showSoftDelete;
        private Type _type;
        /// <summary>
        /// Gets or sets entity config identifier
        /// </summary>
        public string Identifier { get; set; }
        /// <summary>
        /// Gets or sets entity config identifier
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
                Metadata = new DomainEntityMetadata(_type, _showSoftDelete);
            }
        }
        /// <summary>
        /// Gets or sets event keys for the entity. 
        /// These keys are used to uniquly identify CRUD operation events on the entity. 
        /// </summary>
        public EventKeyRecord EventKeys { get; set; }
        /// <summary>
        /// Gets or sets value to expose/hide ISoftDeleted object when ISoftDeleted.Deleted is true
        /// Default is false
        /// </summary>
        public bool ShowSoftDelete
        {
            get => _showSoftDelete;
            set
            {
                _showSoftDelete = value;
                if (_type != null)
                    Metadata = new DomainEntityMetadata(_type, _showSoftDelete);
            }
        }
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
        /// Gets or sets the object that sets up entity controller's behavior
        /// </summary>
        public EndpointSettings EndpointSettings { get; set; }
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