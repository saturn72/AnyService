using System;
using AnyService.Audity;
using AnyService.Security;
using AnyService.Services;
using Microsoft.AspNetCore.Http;

namespace AnyService
{
    public class EntityConfigRecord
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets route for entity.
        /// </summary>
        public PathString Route { get; set; }
        /// <summary>
        /// Gets or sets entity type 
        /// </summary>
        public Type Type { get; set; }
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
        /// Gets or sets value indicating whether the get operation is public to all users or just to the creator
        /// </summary>
        public bool PublicGet { get; set; }
        public Type CrudValidatorType { get; set; }
        public Type ResponseMapperType { get; set; }
        public AuthorizationInfo Authorization { get; set; }
        /// <summary>
        /// Gets or sets paginate default settings
        /// </summary>
        public PaginationSettings PaginationSettings { get; set; }
        public Type FilterFactoryType { get; set; }
        public Type ModelPrepararType { get; set; }
        public Type ControllerType { get; set; }
        public string Area { get; set; }
        /// <summary>
        /// Gets or sets the audit rules for the entity
        /// </summary>
        public AuditRules AuditRules { get; set; }
        internal AuditSettings AuditConfig { get; set; }
    }
}