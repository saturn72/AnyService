using AnyService;
using AnyService.Events;
using AnyService.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnyService.Services.EntityMapping
{
    public class EntityMappingRecordManager : IEntityMappingRecordManager
    {
        #region fields
        private readonly IEnumerable<EntityConfigRecord> _entityConfigRecords;
        private readonly IPermissionManager _permissionManager;
        private readonly IRepository<EntityMappingRecord> _repository;
        private readonly IEventBus _eventBus;
        private readonly WorkContext _workContext;
        private readonly ILogger<EntityMappingRecordManager> _logger;
        #endregion
        #region ctor
        public EntityMappingRecordManager(
            IServiceProvider serviceProvider,
            ILogger<EntityMappingRecordManager> logger)
        {
            _entityConfigRecords = serviceProvider.GetService<IEnumerable<EntityConfigRecord>>();
            _workContext = serviceProvider.GetService<WorkContext>();
            _permissionManager = serviceProvider.GetService<IPermissionManager>();
            _repository = serviceProvider.GetService<IRepository<EntityMappingRecord>>();
            _eventBus = serviceProvider.GetService<IEventBus>();
            _logger = logger;
        }
        #endregion
        #region UpdateMappings
        public async Task<ServiceResponse<EntityMappingRequest>> UpdateMapping(EntityMappingRequest request)
        {
            _logger.LogInformation(LoggingEvents.BusinessLogicFlow, $"Start {nameof(UpdateMapping)} with parameters: {nameof(request)} = {request.ToJsonString()}");
            var serviceResponse = new ServiceResponse<EntityMappingRequest>();
            var parentEcr = _entityConfigRecords.FirstOrDefault(e => e.EntityKey.Equals(request.ParentEntityKey, StringComparison.InvariantCultureIgnoreCase));
            var childEcr = _entityConfigRecords.FirstOrDefault(e => e.EntityKey.Equals(request.ChildEntityKey, StringComparison.InvariantCultureIgnoreCase));

            if (parentEcr == default || parentEcr.EntityMappingSettings.DisabledAsParent ||
                childEcr == default || childEcr.EntityMappingSettings.DisabledAsChild)
            {
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Fail to find matching {nameof(EntityConfigRecord)} for {request.ParentId} OR {request.ChildEntityKey}");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                serviceResponse.Message = $"Fail to find matching {nameof(EntityConfigRecord)} for {request.ParentId} OR {request.ChildEntityKey}";
                return serviceResponse;
            }

            var isPermittedForParent = await _permissionManager.UserHasPermissionOnEntity(_workContext.CurrentUserId, parentEcr.EntityKey, parentEcr.PermissionRecord.UpdateKey, request.ParentId);
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Permission on parent entity = {isPermittedForParent}");
            bool isPermittedForChilds = false;
            if (isPermittedForParent)
            {
                var permittedIds = await _permissionManager.GetPermittedEntitiesIds(_workContext.CurrentUserId, childEcr.EntityKey, childEcr.PermissionRecord.UpdateKey);
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"All child permitted entity ids = {permittedIds.ToJsonString()}");

                var allIds = new List<string>(request.Add ?? new string[] { });
                allIds.AddRange(request.Remove ?? new string[] { });
                isPermittedForChilds = allIds.Distinct().All(s => permittedIds.Contains(s));
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Permission on ALL child entities = {isPermittedForChilds}");
            }

            if (!isPermittedForChilds || !isPermittedForChilds)
            {
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"User is not authorized to perform the request");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                serviceResponse.Message = $"User is not authorized to perform the request";
                return serviceResponse;
            }

            var toRemove = request.Remove ?? new string[] { };
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Delete exists mapping using ids {toRemove?.ToJsonString()}");
            if (!toRemove.IsNullOrEmpty())
            {
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Call {nameof(_repository.BulkDelete)} entity mapping with Ids: {toRemove}");
                var deleted = await _repository.BulkDelete(toRemove.Select(c => new EntityMappingRecord { Id = c }).ToArray());
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(_repository.BulkDelete)} response with collection: {deleted.ToJsonString()}");
            }

            var toAdd = request.Add?.Select(cId => new EntityMappingRecord
            {
                ParentEntityName = parentEcr.EntityKey,
                ParentId = request.ParentId,
                ChildEntityName = childEcr.EntityKey,
                ChildId = cId,
            })?.ToArray() ?? new EntityMappingRecord[] { };

            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Adding new mapping with data {toAdd?.ToJsonString()}");

            if (!toAdd.IsNullOrEmpty())
            {
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Call {nameof(_repository.BulkInsert)} entity mapping with data {toAdd?.ToJsonString()}");
                var added = await _repository.BulkInsert(toAdd, true);
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(_repository.BulkInsert)} response with collection: {added.ToJsonString()}");
            }
            var ded = new DomainEventData
            {
                PerformedByUserId = _workContext.CurrentUserId,
                PerformedUsingClientId = _workContext.CurrentClientId,
                Data = new { mapping = request },
                WorkContext = _workContext
            };
            _eventBus.Publish(parentEcr.EventKeys.Update, ded);
            serviceResponse.Result = ServiceResult.Ok;
            serviceResponse.Payload = request;
            return serviceResponse;
        }
        #endregion
    }
}
