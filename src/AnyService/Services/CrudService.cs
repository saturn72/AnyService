using System;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Security;
using AnyService.Events;
using AnyService.Services.FileStorage;
using AnyService.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AnyService.Services.Audit;
using AnyService.Services.Preparars;
using System.Collections.Generic;
using AnyService.Services.Internals;

namespace AnyService.Services
{
    public class CrudService<TDomainEntity> : ICrudService<TDomainEntity> where TDomainEntity : IDomainEntity
    {
        #region fields
        private static IEnumerable<string> AllAggregatedNames;

        protected readonly IServiceProvider ServiceProvider;
        protected readonly AnyServiceConfig Config;
        protected readonly IRepository<TDomainEntity> Repository;
        protected readonly IRepository<EntityMapping> MapRepository;
        protected readonly CrudValidatorBase<TDomainEntity> Validator;
        protected readonly IModelPreparar<TDomainEntity> ModelPreparar;
        protected readonly WorkContext WorkContext;
        protected readonly IEventBus EventBus;
        protected readonly EventKeyRecord EventKeys;
        protected readonly IFileStoreManager FileStorageManager;
        protected readonly ILogger<CrudService<TDomainEntity>> Logger;
        protected readonly IIdGenerator IdGenerator;
        protected readonly IFilterFactory FilterFactory;
        protected readonly IPermissionManager PermissionManager;
        protected readonly IAuditManager AuditManager;
        private readonly DomainEntityMetadata EntityMetadata;
        #endregion
        #region ctor
        public CrudService(
            IServiceProvider serviceProvider,
            ILogger<CrudService<TDomainEntity>> logger)
        {
            Logger = logger;
            ServiceProvider = serviceProvider;
            Config = serviceProvider.GetService<AnyServiceConfig>();
            Repository = serviceProvider.GetService<IRepository<TDomainEntity>>();
            MapRepository = serviceProvider.GetService<IRepository<EntityMapping>>();

            Validator = serviceProvider.GetService<CrudValidatorBase<TDomainEntity>>();
            WorkContext = serviceProvider.GetService<WorkContext>();
            ModelPreparar = serviceProvider.GetService<IModelPreparar<TDomainEntity>>();
            EventBus = serviceProvider.GetService<IEventBus>();
            FileStorageManager = serviceProvider.GetService<IFileStoreManager>();
            IdGenerator = serviceProvider.GetService<IIdGenerator>();
            FilterFactory = serviceProvider.GetService<IFilterFactory>();
            PermissionManager = serviceProvider.GetService<IPermissionManager>();
            AuditManager = serviceProvider.GetService<IAuditManager>();
            EventKeys = WorkContext?.CurrentEntityConfigRecord?.EventKeys;
            EntityMetadata = WorkContext?.CurrentEntityConfigRecord?.Metadata;
            AllAggregatedNames ??= EntityMetadata.Aggregations.Select(a => a.EntityName.ToLower());
        }

        #endregion

        public virtual async Task<ServiceResponse<TDomainEntity>> Create(TDomainEntity entity)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start create flow for entity: {entity}");

            var serviceResponse = new ServiceResponse<TDomainEntity>();
            if (!await Validator.ValidateForCreate(entity, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Prepare entity to be inserted to database");
            await ModelPreparar.PrepareForCreate(entity);

            Logger.LogDebug(LoggingEvents.Repository, $"Insert entity to repository");
            if (EntityMetadata.IsSoftDeleted) (entity as ISoftDelete).Deleted = false;

            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var dbData = await Repository.Command(r => r.Insert(entity), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository insert response: {dbData}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Create, entity))
                return serviceResponse;
            if (EntityMetadata.IsCreatableAudit)
                await AuditManager.InsertCreateRecord(entity);

            Publish(EventKeys.Create, serviceResponse.Payload);
            serviceResponse.Result = ServiceResult.Ok;

            //if (entity is IFileContainer)
            //{
            //    Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start file uploads");
            //    await UploadFiles(dbData as IFileContainer, serviceResponse);
            //}
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        //public virtual async Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse)
        //{
        //    var files = fileContainer?.Files;
        //    if (files == null || !files.Any())
        //        return;

        //    foreach (var f in files)
        //    {
        //        f.ParentId = fileContainer.Id;
        //        f.ParentKey = WorkContext.CurrentEntityConfigRecord.EntityKey;
        //    }
        //    var uploadResponses = await FileStorageManager.Upload(files);
        //    serviceResponse.Data = new { entity = serviceResponse.Data, filesUploadStatus = uploadResponses };
        //}
        #region GetById
        public virtual async Task<ServiceResponse<TDomainEntity>> GetById(string id)
        {
            Logger.LogInformation(LoggingEvents.BusinessLogicFlow, $"Start get by id with {nameof(id)} = {id}");

            var serviceResponse = new ServiceResponse<TDomainEntity>();
            if (!await Validator.ValidateForGet(id, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Get by Id from repository");

            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var data = await Repository.Query(r => r.GetById(id), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Read, id))
                return serviceResponse;
            if (!EntityMetadata.ShowSoftDeleted && EntityMetadata.IsSoftDeleted && (data as ISoftDelete).Deleted)
            {
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            if (data != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Payload = data;
                serviceResponse.Result = ServiceResult.Ok;

                if (EntityMetadata.IsReadableAudit)
                    await AuditManager.InsertReadRecord(data);

                Publish(EventKeys.Read, serviceResponse.Payload);
            }
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");

            return serviceResponse;
        }
        #endregion
        #region GetAll
        public virtual async Task<ServiceResponse<Pagination<TDomainEntity>>> GetAll(Pagination<TDomainEntity> pagination)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start get all flow");
            var serviceResponse = new ServiceResponse<Pagination<TDomainEntity>> { Payload = pagination };

            if (!await Validator.ValidateForGet(pagination, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Request did not pass validation");

            if ((pagination = await NormalizePagination(pagination)) == null)
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.BusinessLogicFlow, "Missing query data");

            Logger.LogDebug(LoggingEvents.Repository, "Get all from repository using paginate = " + pagination);
            var wrapper = new ServiceResponseWrapper(new ServiceResponse<IEnumerable<TDomainEntity>>());
            var data = await Repository.Query(r => r.GetAll(pagination), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Read, pagination))
            {
                var wSrvRes = wrapper.ServiceResponse;
                serviceResponse.Message = wSrvRes.Message;
                serviceResponse.ExceptionId = wSrvRes.ExceptionId;
                serviceResponse.Result = wSrvRes.Result;
                return serviceResponse;
            }

            pagination.Data = data ?? new TDomainEntity[] { };
            if (serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Payload = pagination;
                serviceResponse.Result = ServiceResult.Ok;

                if (EntityMetadata.IsReadableAudit)
                    await AuditManager.InsertReadRecord(pagination);

                Publish(EventKeys.Read, serviceResponse.Payload);
            }
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        private async Task<Pagination<TDomainEntity>> NormalizePagination(Pagination<TDomainEntity> pagination)
        {
            var p = pagination ??= new Pagination<TDomainEntity>();

            if (!p.QueryOrFilter.HasValue() && p.QueryFunc == null)
                p.QueryFunc = x => x.Id.HasValue();

            var filter = p.QueryOrFilter.HasValue() ?
                await FilterFactory.GetFilter<TDomainEntity>(p.QueryOrFilter) : null;

            if (filter != null)
            {
                var f = filter(new object());//payload is not supported yet
                if (f == null)
                    return null;
                p.QueryFunc = c => f(c);
            }
            else
            {
                if (p.QueryFunc == null) //build only if func not exists
                {
                    var right = ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainEntity>(p.QueryOrFilter)?.Compile();
                    if (right == null) return null;

                    var ecr = WorkContext.CurrentEntityConfigRecord;
                    if (Config.ManageEntityPermissions)
                    {
                        var permittedIds = await PermissionManager.GetPermittedIds(WorkContext.CurrentUserId, ecr.EntityKey, ecr.PermissionRecord.ReadKey);
                        Func<TDomainEntity, bool> left = a => permittedIds.Contains(a.Id);
                        p.QueryFunc = x => left(x) && right(x);
                    }
                    else
                    {
                        p.QueryFunc = x => right(x);
                    }
                }
            }

            if (p.QueryFunc == null) return null;
            if (!EntityMetadata.ShowSoftDeleted && EntityMetadata.IsSoftDeleted)
                p.QueryFunc = p.QueryFunc.AndAlso(x => !(x as ISoftDelete).Deleted);

            var paginationSettings = WorkContext.CurrentEntityConfigRecord.PaginationSettings;
            p.OrderBy ??= paginationSettings.DefaultOrderBy;
            p.Offset = p.Offset != 0 ? p.Offset : paginationSettings.DefaultOffset;
            p.PageSize = p.PageSize != 0 ? p.PageSize : paginationSettings.DefaultPageSize;
            p.IncludeNested = p.IncludeNested;
            p.SortOrder ??= paginationSettings.DefaultSortOrder;

            return p;
        }
        #endregion
        #region Get Aggregated
        public async Task<ServiceResponse<IReadOnlyDictionary<string, IEnumerable<IDomainEntity>>>> GetAggregated(
            string parentId,
            IEnumerable<string> childEntityNames
            )
        {
            Logger.LogInformation(LoggingEvents.BusinessLogicFlow, $"Start get aggregated with parameters: {nameof(parentId)} = {parentId}, {nameof(childEntityNames)} = {childEntityNames.ToJsonString()}");
            var serviceResponse = new ServiceResponse<IReadOnlyDictionary<string, IEnumerable<IDomainEntity>>>();
            if (
                !parentId.HasValue() ||
                childEntityNames.IsNullOrEmpty() ||
                !AllAggregatedExists())
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Invalid parameters");

                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            var ecrs = ServiceProvider.GetService<IEnumerable<EntityConfigRecord>>();
            var res = new Dictionary<string, IEnumerable<IDomainEntity>>();

            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Get all exists mappings: parent entity name: {WorkContext.CurrentEntityConfigRecord.Name}, {nameof(parentId)} = {parentId}, {nameof(childEntityNames)} = {childEntityNames.ToJsonString()}");
            var groupMaps = await GetGroupedMappingByParentIdAndChildEntityNames(parentId, childEntityNames);
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Returns mapping: {groupMaps}");

            foreach (var gm in groupMaps)
            {
                var ecr = ecrs.FirstOrDefault(e => e.Name.ToLower() == gm.Key.ToLower());
                if (ecr == default)
                    continue;

                dynamic r = ServiceProvider.GetGenericService(typeof(IRepository<>), ecr.Type);
                if (r == null)
                {
                    Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Generic type {nameof(IRepository<object>)} for EntityConfigRecord {nameof(ecr.Name)} not defined");
                    serviceResponse.Result = ServiceResult.Error;
                    return serviceResponse;
                }
                var curChildIds = gm.Select(s => s.ChildId);
                var curCol = (await r.Collection) as IQueryable<IDomainEntity>;
                var aggregated = curCol.Where(c => curChildIds.Contains(c.Id));
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Add to returned collection: {nameof(gm.Key)} = {gm.Key}, Value = {aggregated.ToJsonString()}");
                res[gm.Key] = aggregated.ToArray();
            }

            serviceResponse.Result = ServiceResult.Ok;
            serviceResponse.Payload = res;

            Publish(EventKeys.Read, serviceResponse.Payload);
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(ServiceResponse)} = {serviceResponse.ToJsonString()}");

            return serviceResponse;

            bool AllAggregatedExists()
            {
                childEntityNames = childEntityNames.Select(x => x.Trim().ToLower());
                var res = AllAggregatedNames.All(childEntityNames.Contains);
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{childEntityNames}{(res ? "" : " NOT")} exists in {AllAggregatedNames.ToJsonString()}");

                return res;
            }
        }
        public async Task<ServiceResponse<Pagination<TChild>>> GetAggregatedPage<TChild>(string parentId, Pagination<TChild> pagination, string childEntityName = null)
            where TChild : IDomainEntity
        {
            Logger.LogInformation(LoggingEvents.BusinessLogicFlow, $"Start {nameof(GetAggregatedPage)} with parameters: {nameof(parentId)} = {parentId}, {nameof(pagination)} = {pagination.ToJsonString()}, {nameof(childEntityName)} = {childEntityName}");

            var serviceResponse = new ServiceResponse<Pagination<TChild>> { Payload = pagination };
            var genericTypeArgument = pagination?.GetType().GenericTypeArguments[0];

            var ecrs = ServiceProvider.GetService<IEnumerable<EntityConfigRecord>>();
            var ecr = childEntityName.HasValue() ?
                ecrs.FirstOrDefault(e => e.Name == childEntityName) :
                ecrs.FirstOrDefault(e => e.Type == genericTypeArgument);

            if (ecr == null)
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(EntityConfigRecord)} of type {nameof(genericTypeArgument.Name)} OR named {nameof(childEntityName)} is not configures");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Get all exists mappings: parent entity name: {WorkContext.CurrentEntityConfigRecord.Name}, {nameof(parentId)} = {parentId}, {nameof(childEntityName)} = {ecr.Name}");
            var groupMaps = await GetGroupedMappingByParentIdAndChildEntityNames(parentId, new[] { ecr.Name });
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Returns mapping: {groupMaps}");
            if (groupMaps.IsNullOrEmpty())
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{ServiceResult.BadOrMissingData} - no mappings found");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            var childIds = groupMaps.First().Select(s => s.ChildId);
            dynamic r = ServiceProvider.GetGenericService(typeof(IRepository<>), ecr.Type);
            if (r == null)
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Generic type {nameof(IRepository<object>)} for EntityConfigRecord {nameof(ecr.Name)} not defined");
                serviceResponse.Result = ServiceResult.Error;
                return serviceResponse;
            }

            pagination.QueryFunc = new Func<TChild, bool>(s => childIds.Contains(s.Id));
            var data = await r.GetAll(pagination) as IEnumerable<TChild>;
            pagination.Data = data;
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Get paged data = {data.ToJsonString()}");
            serviceResponse.Result = ServiceResult.Ok;

            Publish(EventKeys.Read, pagination);
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(ServiceResponse)} = {serviceResponse}");
            return serviceResponse;
        }
        private async Task<IEnumerable<IGrouping<string, EntityMapping>>> GetGroupedMappingByParentIdAndChildEntityNames(string parentId, IEnumerable<string> childEntityNames)
        {
            var col = await MapRepository.Collection;
            var res = col.Where(p())
                .GroupBy(g => g.ChildEntityName)
                .Select(x => x);

            return res;

            Func<EntityMapping, bool> p() => c =>
                c.ParentEntityName == WorkContext.CurrentEntityConfigRecord.Name &&
                   c.ParentId == parentId &&
                   childEntityNames.Contains(c.ChildEntityName, StringComparer.InvariantCultureIgnoreCase);
            //return from c in col
            //       where
            //       c.ParentEntityName == WorkContext.CurrentEntityConfigRecord.Name &&
            //       c.ParentId == parentId &&
            //       childEntityNames.Contains(c.ChildEntityName, StringComparer.InvariantCultureIgnoreCase)
            //       group c by c.ChildEntityName into g
            //       select g;
        }
        #endregion
        #region UpdateMappings
        public async Task<ServiceResponse<EntityMappingResponse>> UpdateMappings(EntityMappingRequest request)
        {
            Logger.LogInformation(LoggingEvents.BusinessLogicFlow, $"Start {nameof(UpdateMappings)} with parameters: {nameof(request)} = {request.ToJsonString()}");
            var serviceResponse = new ServiceResponse<EntityMappingResponse>();
            if (request == null)
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(request)} is null");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            var ecrs = ServiceProvider.GetService<IEnumerable<EntityConfigRecord>>();
            var ecr = ecrs.FirstOrDefault(e => e.Name.ToLower() == request.ChildEntityName.ToLower());

            if (ecr == null)
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(EntityConfigRecord)} of named {nameof(request.ChildEntityName)} is not configures");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            dynamic r = ServiceProvider.GetGenericService(typeof(IRepository<>), ecr.Type);
            if (r == null)
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Generic type {nameof(IRepository<object>)} for EntityConfigRecord {nameof(ecr.Name)} not defined");
                serviceResponse.Result = ServiceResult.Error;
                return serviceResponse;
            }
            var curCol = (await r.Collection) as IQueryable<IDomainEntity>;
            var entries = curCol.Where(c => request.ChildIdsToAdd.Contains(c.Id));
            if (entries.Count() != request.ChildIdsToAdd.Count())
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Missing entities in persistency layer. matching entities: {request.ChildIdsToAdd.ToJsonString()}, required entities: {entries.ToJsonString()}");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Get all exists mappings: parent entity name: {WorkContext.CurrentEntityConfigRecord.Name}, {nameof(request.ParentId)} = {request.ParentId}, {nameof(request.ChildEntityName)} = {ecr.Name}");
            var groupMaps = await GetGroupedMappingByParentIdAndChildEntityNames(request.ParentId, new[] { ecr.Name });
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Returns mapping: {groupMaps}");

            var toDelete = groupMaps.FirstOrDefault()?
                .Where(e => request.ChildIdsToRemove.Contains(e.ChildId))?
                .ToArray() ?? new EntityMapping[] { };
            //delete
            if (!toDelete.IsNullOrEmpty())
            {
                var idsToDelete = toDelete.Select(s => s.Id).ToArray();
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(IRepository<string>.BulkDelete)} entity mapping with Ids: {idsToDelete}");
                await MapRepository.BulkDelete(toDelete);
                Publish(EventKeys.Delete, toDelete);
            }

            var toAdd = request.ChildIdsToAdd?.Select(cId => new EntityMapping
            {
                ParentEntityName = WorkContext.CurrentEntityConfigRecord.Name,
                ParentId = request.ParentId,
                ChildEntityName = ecr.Name,
                ChildId = cId,
            })?.ToArray() ?? new EntityMapping[] { };

            if (!toAdd.IsNullOrEmpty())
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(IRepository<string>.BulkInsert)} insert entity mapping: {toAdd.ToJsonString()}");
                await MapRepository.BulkInsert(toAdd, true);
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"{nameof(IRepository<string>.BulkInsert)} response with collection: {toAdd.ToJsonString()}");
                Publish(EventKeys.Create, toAdd);
            }
            serviceResponse.Result = ServiceResult.Ok;
            serviceResponse.Payload = new EntityMappingResponse(request)
            {
                EntityMappingsAdded = toAdd,
                EntityMappingsRemoved = toDelete
            };
            return serviceResponse;
        }
        #endregion 
        #region Update
        public virtual async Task<ServiceResponse<TDomainEntity>> Update(string id, TDomainEntity entity)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start update flow for id: {id}, entity: {entity}");
            entity.Id = id;
            var serviceResponse = new ServiceResponse<TDomainEntity>();

            if (!await Validator.ValidateForUpdate(entity, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Repository - Fetch entity");
            var wrapper = new ServiceResponseWrapper(serviceResponse);

            var dbEntry = await Repository.Query(async r => await r.GetById(id), wrapper);
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Update, id))
                return serviceResponse;

            if (EntityMetadata.IsSoftDeleted && (dbEntry as ISoftDelete).Deleted)
            {
                Logger.LogDebug(LoggingEvents.Audity, "entity already deleted");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }
            await ModelPreparar.PrepareForUpdate(dbEntry, entity);

            Logger.LogDebug(LoggingEvents.Repository, $"Update entity in repository");
            var updateResponse = await Repository.Command(r => r.Update(entity), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository update response: {updateResponse}");
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Update, entity))
                return serviceResponse;

            //if (entity is IFileContainer)
            //{
            //    var fileContainer = (entity as IFileContainer);
            //    await FileStorageManager.Delete((dbEntry as IFileContainer).Files);
            //    (updateResponse as IFileContainer).Files = fileContainer.Files;
            //    Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start file uploads");
            //    await UploadFiles(fileContainer, serviceResponse);
            //}

            if (EntityMetadata.IsUpdatableAudit)
                await AuditManager.InsertUpdatedRecord(dbEntry, entity);

            Publish(EventKeys.Update, serviceResponse.Payload);
            serviceResponse.Result = ServiceResult.Ok;
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        #endregion
        #region Delete
        public virtual async Task<ServiceResponse<TDomainEntity>> Delete(string id)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start delete flow for id: {id}");
            var serviceResponse = new ServiceResponse<TDomainEntity>();

            if (!await Validator.ValidateForDelete(id, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Repository - Fetch entity");
            var wrapper = new ServiceResponseWrapper(serviceResponse);

            var dbEntry = await Repository.Query(r => r.GetById(id), wrapper);
            if (dbEntry == null)
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.BusinessLogicFlow, "Entity not exists");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Delete, id))
                return serviceResponse;

            if (EntityMetadata.IsSoftDeleted && (dbEntry as ISoftDelete).Deleted)
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.BusinessLogicFlow, "Entity already deleted");

            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Prepare for deletion");
            await ModelPreparar.PrepareForDelete(dbEntry);

            TDomainEntity deletedModel;
            if (EntityMetadata.IsSoftDeleted)
            {
                (dbEntry as ISoftDelete).Deleted = true;
                Logger.LogDebug(LoggingEvents.Repository, $"Repository - soft deletion (update) {nameof(ISoftDelete)} entity");
                deletedModel = await Repository.Command(r => r.Update(dbEntry), wrapper);
            }
            else
            {
                Logger.LogDebug(LoggingEvents.Repository, "Repository - delete entity with id " + id);
                deletedModel = await Repository.Command(r => r.Delete(dbEntry), wrapper);
            }

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Delete, id))
                return serviceResponse;

            if (EntityMetadata.IsDeletableAudit)
                await AuditManager.InsertDeletedRecord(dbEntry);

            Publish(EventKeys.Delete, serviceResponse.Payload);
            serviceResponse.Result = ServiceResult.Ok;
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        #endregion
        #region Utilities
        private ServiceResponse<T> SetServiceResponse<T>(ServiceResponse<T> serviceResponse, string serviceResponseResult, EventId eventId, string logMessage)
        {
            Logger.LogDebug(eventId, logMessage);
            serviceResponse.Result = serviceResponseResult;
            return serviceResponse;
        }
        private bool IsNotFoundOrBadOrMissingDataOrError(ServiceResponseWrapper wrapper, string eventKey, object requestData)
        {
            var serviceResponse = wrapper.ServiceResponse;
            Logger.LogDebug(LoggingEvents.Repository, $"Has {serviceResponse.Result} response");
            if (serviceResponse.Result == ServiceResult.BadOrMissingData || serviceResponse.Result == ServiceResult.NotFound)
            {
                return true;
            }
            if (wrapper.Exception != null || serviceResponse.Result == ServiceResult.Error)
            {
                Logger.LogDebug(LoggingEvents.Repository, "Repository response is null");
                PublishException(serviceResponse, eventKey, requestData, wrapper.Exception);
                return true;
            }
            return false;
        }
        private void Publish(string eventKey, object data)
        {
            Logger.LogDebug(LoggingEvents.EventPublishing, $"Publish event using {eventKey} key");
            EventBus.Publish(eventKey, new DomainEventData
            {
                Data = data,
                PerformedByUserId = WorkContext.CurrentUserId,
                WorkContext = WorkContext
            });
        }
        private void PublishException(ServiceResponse serviceResponse, string eventKey, object data, Exception exception)
        {
            serviceResponse.ExceptionId = IdGenerator.GetNext();
            Logger.LogDebug(LoggingEvents.Repository, $"Repository returned with exception. exceptionId: {serviceResponse.ExceptionId}");

            var exceptionData = new
            {
                incomingObject = data,
                exceptionId = serviceResponse.ExceptionId,
                exception = exception
            };
            Publish(eventKey, exceptionData);
        }
        #endregion
    }
}
