using System;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Security;
using AnyService.Events;
using AnyService.Services.FileStorage;
using Microsoft.Extensions.Logging;
using AnyService.Services.Preparars;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace AnyService.Services
{
    public class CrudService<TEntity> : ICrudService<TEntity> where TEntity : IEntity
    {
        #region fields
        private static readonly object lockObj = new object();
        private static readonly Func<TEntity, bool> HideSoftDeletedFunc = x => !(x as ISoftDelete).Deleted;

        protected readonly AnyServiceConfig Config;
        protected readonly IRepository<TEntity> Repository;
        protected readonly CrudValidatorBase<TEntity> Validator;
        protected readonly IModelPreparar<TEntity> ModelPreparar;
        protected readonly WorkContext WorkContext;
        protected readonly IEventBus EventBus;
        protected readonly IFileStoreManager FileStorageManager;
        protected readonly ILogger<CrudService<TEntity>> Logger;
        protected readonly IFilterFactory FilterFactory;
        protected readonly IPermissionManager PermissionManager;
        private readonly EntityConfigRecord CurrentEntityConfigRecord;
        private IEnumerable<string> _propertyNames;
        #endregion
        #region ctor
        public CrudService(
            AnyServiceConfig config,
            IRepository<TEntity> repository,
            CrudValidatorBase<TEntity> validator,
            WorkContext workContext,
            IModelPreparar<TEntity> modelPreparar,
            IEventBus eventBus,
            IFileStoreManager fileStoreManager,
            IFilterFactory filterFactory,
            IPermissionManager permissionManager,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            ILogger<CrudService<TEntity>> logger)
        {
            Logger = logger;
            Config = config;
            Repository = repository;
            Validator = validator;
            WorkContext = workContext;
            ModelPreparar = modelPreparar;
            EventBus = eventBus;
            FileStorageManager = fileStoreManager;
            FilterFactory = filterFactory;
            PermissionManager = permissionManager;
            CurrentEntityConfigRecord = entityConfigRecords.First(typeof(TEntity));
        }

        #endregion

        public virtual async Task<ServiceResponse<TEntity>> Create(TEntity entity)
        {
            Logger.LogInformation(LoggingEvents.BusinessLogicFlow, $"Start create flow for entity: {entity}");

            var serviceResponse = new ServiceResponse<TEntity>();
            if (!await Validator.ValidateForCreate(entity, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Prepare entity to be inserted to database");
            await ModelPreparar.PrepareForCreate(entity);

            Logger.LogDebug(LoggingEvents.Repository, $"Insert entity to repository");
            if (CurrentEntityConfigRecord.Metadata.IsSoftDeleted) (entity as ISoftDelete).Deleted = false;

            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var dbData = await Repository.Command(r => r.Insert(entity), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository insert response: {dbData}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, CurrentEntityConfigRecord.EventKeys.Create, entity))
                return serviceResponse;

            Publish(CurrentEntityConfigRecord.EventKeys.Create, serviceResponse.Payload);
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
        public virtual async Task<ServiceResponse<TEntity>> GetById(string id)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start get by id with id = {id}");

            var serviceResponse = new ServiceResponse<TEntity>();
            if (!await Validator.ValidateForGet(id, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Get by Id from repository");

            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var data = await Repository.Query(r => r.GetById(id), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, CurrentEntityConfigRecord.EventKeys.Read, id))
                return serviceResponse;
            if (!CurrentEntityConfigRecord.Metadata.ShowSoftDeleted && CurrentEntityConfigRecord.Metadata.IsSoftDeleted && (data as ISoftDelete).Deleted)
            {
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            if (data != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Payload = data;
                serviceResponse.Result = ServiceResult.Ok;

                Publish(CurrentEntityConfigRecord.EventKeys.Read, serviceResponse.Payload);
            }
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public virtual async Task<ServiceResponse<Pagination<TEntity>>> GetAll(Pagination<TEntity> pagination)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start get all flow");
            var serviceResponse = new ServiceResponse<Pagination<TEntity>> { Payload = pagination };

            if (!await Validator.ValidateForGet(pagination, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Request did not pass validation");

            if ((pagination = await NormalizePagination(pagination)) == null)
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.BusinessLogicFlow, "Missing query data");

            Logger.LogDebug(LoggingEvents.Repository, "Get all from repository using paginate = " + pagination);
            var wrapper = new ServiceResponseWrapper(new ServiceResponse<IEnumerable<TEntity>>());
            var data = await Repository.Query(r => r.GetAll(pagination), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, CurrentEntityConfigRecord.EventKeys.Read, pagination))
            {
                var wSrvRes = wrapper.ServiceResponse;
                serviceResponse.Message = wSrvRes.Message;
                serviceResponse.TraceId = wSrvRes.TraceId;
                serviceResponse.Result = wSrvRes.Result;
                return serviceResponse;
            }

            pagination.Data = data ?? new TEntity[] { };
            if (serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Payload = pagination;
                serviceResponse.Result = ServiceResult.Ok;
                Expression<Func<TEntity, bool>> exp = x => pagination.QueryFunc(x);
                pagination.QueryOrFilter = exp.ToString();

                Publish(CurrentEntityConfigRecord.EventKeys.Read, pagination);
            }
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        private async Task<Pagination<TEntity>> NormalizePagination(Pagination<TEntity> pagination)
        {
            var p = pagination ??= new Pagination<TEntity>();

            if (!p.QueryOrFilter.HasValue() && p.QueryFunc == null)
                p.QueryFunc = x => true;

            var filter = p.QueryOrFilter.HasValue() ?
                await FilterFactory.GetFilter<TEntity>(p.QueryOrFilter) : null;

            if (!p.ProjectedFields.IsNullOrEmpty())
            {
                p.ProjectedFields = MatchProjectedFields(p);
                if (p.ProjectedFields.IsNullOrEmpty())
                    return null;
            }

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
                    var right = ExpressionTreeBuilder.BuildBinaryTreeExpression<TEntity>(p.QueryOrFilter)?.Compile();
                    if (right == null) return null;

                    var ecr = WorkContext.CurrentEntityConfigRecord;
                    if (Config.ManageEntityPermissions)
                    {
                        var permittedIds = await PermissionManager.GetPermittedIds(WorkContext.CurrentUserId, ecr.EntityKey, ecr.PermissionRecord.ReadKey);
                        Func<TEntity, bool> left = a => permittedIds.Contains(a.Id);
                        p.QueryFunc = x => left(x) && right(x);
                    }
                    else
                    {
                        p.QueryFunc = x => right(x);
                    }
                }
            }

            if (p.QueryFunc == null) return null;
            if (!CurrentEntityConfigRecord.Metadata.ShowSoftDeleted && CurrentEntityConfigRecord.Metadata.IsSoftDeleted)
                p.QueryFunc = p.QueryFunc.AndAlso(HideSoftDeletedFunc);

            var paginationSettings = WorkContext.CurrentEntityConfigRecord.PaginationSettings;
            p.OrderBy ??= paginationSettings.DefaultOrderBy;
            p.Offset = p.Offset != 0 ? p.Offset : paginationSettings.DefaultOffset;
            p.PageSize = p.PageSize != 0 ? p.PageSize : paginationSettings.DefaultPageSize;
            p.IncludeNested = p.IncludeNested;
            p.SortOrder ??= paginationSettings.DefaultSortOrder;

            return p;
        }

        private IEnumerable<string> MatchProjectedFields(Pagination<TEntity> p)
        {
            _propertyNames ??= new ReadOnlyCollection<string>(CurrentEntityConfigRecord.Type.GetProperties().Select(p => p.Name).ToList());

            var normalizedProjectedFileds = new List<string>();
            foreach (var pf in p.ProjectedFields)
            {
                var x = _propertyNames.FirstOrDefault(x => x.Equals(pf.Trim(), StringComparison.InvariantCultureIgnoreCase));
                if (x == null)
                    return null;
                normalizedProjectedFileds.Add(x);
            }
            return normalizedProjectedFileds;
        }

        public virtual async Task<ServiceResponse<TEntity>> Update(string id, TEntity entity)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start update flow for id: {id}, entity: {entity}");
            entity.Id = id;
            var serviceResponse = new ServiceResponse<TEntity>();

            if (!await Validator.ValidateForUpdate(entity, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Repository - Fetch entity");
            var wrapper = new ServiceResponseWrapper(serviceResponse);

            var dbEntry = await Repository.Query(async r => await r.GetById(id), wrapper);
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, CurrentEntityConfigRecord.EventKeys.Update, id))
                return serviceResponse;

            if (CurrentEntityConfigRecord.Metadata.IsSoftDeleted && (dbEntry as ISoftDelete).Deleted)
            {
                Logger.LogDebug(LoggingEvents.Audity, "entity already deleted");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }
            await ModelPreparar.PrepareForUpdate(dbEntry, entity);

            Logger.LogDebug(LoggingEvents.Repository, $"Update entity in repository");
            var updateResponse = await Repository.Command(r => r.Update(entity), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository update response: {updateResponse}");
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, CurrentEntityConfigRecord.EventKeys.Update, entity))
                return serviceResponse;

            //if (entity is IFileContainer)
            //{
            //    var fileContainer = (entity as IFileContainer);
            //    await FileStorageManager.Delete((dbEntry as IFileContainer).Files);
            //    (updateResponse as IFileContainer).Files = fileContainer.Files;
            //    Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start file uploads");
            //    await UploadFiles(fileContainer, serviceResponse);
            //}
            Publish(CurrentEntityConfigRecord.EventKeys.Update, new EntityUpdatedDomainEvent(dbEntry, entity));
            serviceResponse.Result = ServiceResult.Ok;
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public virtual async Task<ServiceResponse<TEntity>> Delete(string id)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start delete flow for id: {id}");
            var serviceResponse = new ServiceResponse<TEntity>();

            if (!await Validator.ValidateForDelete(id, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Repository - Fetch entity");
            var wrapper = new ServiceResponseWrapper(serviceResponse);

            var dbEntry = await Repository.Query(r => r.GetById(id), wrapper);
            if (dbEntry == null)
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.BusinessLogicFlow, "Entity not exists");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, CurrentEntityConfigRecord.EventKeys.Delete, id))
                return serviceResponse;

            if (CurrentEntityConfigRecord.Metadata.IsSoftDeleted && (dbEntry as ISoftDelete).Deleted)
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.BusinessLogicFlow, "Entity already deleted");

            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Prepare for deletion");
            await ModelPreparar.PrepareForDelete(dbEntry);

            TEntity deletedModel;
            if (CurrentEntityConfigRecord.Metadata.IsSoftDeleted)
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

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, CurrentEntityConfigRecord.EventKeys.Delete, id))
                return serviceResponse;

            Publish(CurrentEntityConfigRecord.EventKeys.Delete, serviceResponse.Payload);
            serviceResponse.Result = ServiceResult.Ok;
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

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
                Logger.LogDebug(LoggingEvents.Repository, $"Repository returned with exception. {nameof(ServiceResponse.TraceId)}: {serviceResponse.TraceId}");
                serviceResponse.TraceId = WorkContext.TraceId;

                EventBus.PublishException(eventKey, wrapper.Exception, requestData, WorkContext);

                return true;
            }
            return false;
        }

        private void Publish(string eventKey, object data)
        {
            Logger.LogDebug(LoggingEvents.EventPublishing, $"Publish event using {eventKey} key");
            EventBus.Publish(eventKey, new DomainEvent
            {
                Data = data,
                PerformedByUserId = WorkContext.CurrentUserId,
                WorkContext = WorkContext
            });
        }
        #endregion
    }
}
