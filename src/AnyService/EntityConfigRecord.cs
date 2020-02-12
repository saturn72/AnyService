using System;
using AnyService.Core.Security;
using AnyService.Services;

namespace AnyService
{
    public class EntityConfigRecord
    {
        /// <summary>
        /// Gets or sets route for entity.
        /// </summary>
        public string Route { get; set; }
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
        public ICrudValidator Validator { get; set; }
        public AuthorizationInfo Authorization { get; set; }
    }
}