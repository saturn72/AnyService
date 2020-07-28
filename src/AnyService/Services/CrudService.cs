using System;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Security;
using AnyService.Events;
using AnyService.Services.FileStorage;
using AnyService.Utilities;
using Microsoft.Extensions.Logging;

namespace AnyService.Services
{
    public class CrudService<TDomainModel> : ICrudService<TDomainModel> where TDomainModel : IDomainModelBase
    {
        #region fields
        protected readonly AnyServiceConfig Config;
        protected readonly IRepository<TDomainModel> Repository;
        protected readonly CrudValidatorBase<TDomainModel> Validator;
        protected readonly IModelPreparar<TDomainModel> ModelPreparar;
        protected readonly WorkContext WorkContext;
        protected readonly IEventBus EventBus;
        protected readonly EventKeyRecord EventKeys;
        protected readonly IFileStoreManager FileStorageManager;
        protected readonly ILogger<CrudService<TDomainModel>> Logger;
        protected readonly IIdGenerator IdGenerator;
        protected readonly IFilterFactory FilterFactory;
        protected readonly IPermissionManager PermissionManager;
        #endregion
        #region ctor
        public CrudService(
            AnyServiceConfig config,
            IRepository<TDomainModel> repository,
            ICrudValidator validator,
            IModelPreparar<TDomainModel> modelPreparar,
            WorkContext workContext,
            IEventBus eventBus,
            IFileStoreManager fileStorageManager,
            ILogger<CrudService<TDomainModel>> logger,
            IIdGenerator idGenerator,
            IFilterFactory filterFactory,
            IPermissionManager permissionManager)
        {
            Repository = repository;
            Validator = validator as CrudValidatorBase<TDomainModel>;
            ModelPreparar = modelPreparar;
            WorkContext = workContext;
            EventBus = eventBus;
            EventKeys = workContext?.CurrentEntityConfigRecord?.EventKeys;
            FileStorageManager = fileStorageManager;
            Logger = logger;
            IdGenerator = idGenerator;
            FilterFactory = filterFactory;
            PermissionManager = permissionManager;
            Config = config;
        }

        #endregion
        public virtual async Task<ServiceResponse> Create(TDomainModel entity)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start create flow for entity: {entity}");

            var serviceResponse = new ServiceResponse();
            if (!await Validator.ValidateForCreate(entity, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Prepare entity to be inserted to database");
            await ModelPreparar.PrepareForCreate(entity);

            Logger.LogDebug(LoggingEvents.Repository, $"Insert entity to repository");

            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var dbData = await Repository.Command(r => r.Insert(entity), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository insert response: {dbData}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Create, entity))
                return serviceResponse;

            Publish(EventKeys.Create, serviceResponse.Data);
            serviceResponse.Result = ServiceResult.Ok;

            if (entity is IFileContainer)
            {
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start file uploads");
                await UploadFiles(dbData as IFileContainer, serviceResponse);
            }
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        public virtual async Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse)
        {
            var files = fileContainer?.Files;
            if (files == null || !files.Any())
                return;

            foreach (var f in files)
            {
                f.ParentId = fileContainer.Id;
                f.ParentKey = WorkContext.CurrentEntityConfigRecord.EntityKey;
            }
            var uploadResponses = await FileStorageManager.Upload(files);
            serviceResponse.Data = new { entity = serviceResponse.Data, filesUploadStatus = uploadResponses };
        }
        public virtual async Task<ServiceResponse> GetById(string id)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start get by id with id = {id}");

            var serviceResponse = new ServiceResponse
            {
                Data = id
            };
            if (!await Validator.ValidateForGet(serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Get by Id from repository");

            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var data = await Repository.Query(r => r.GetById(id), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Read, id))
                return serviceResponse;

            if (data != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Data = data;
                serviceResponse.Result = ServiceResult.Ok;
                Publish(EventKeys.Read, serviceResponse.Data);
            }
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public virtual async Task<ServiceResponse> GetAll(Pagination<TDomainModel> pagination)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start get all flow");
            var serviceResponse = new ServiceResponse { Data = pagination };

            if (!await Validator.ValidateForGet(serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Request did not pass validation");

            if ((pagination = await NormalizePagination(pagination)) == null)
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.BusinessLogicFlow, "Missing query data");

            Logger.LogDebug(LoggingEvents.Repository, "Get all from repository using paginate = " + pagination);
            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var data = await Repository.Query(r => r.GetAll(pagination), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Read, pagination))
                return serviceResponse;
            pagination.Data = data ?? new TDomainModel[] { };
            if (serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Data = pagination;
                serviceResponse.Result = ServiceResult.Ok;
                Publish(EventKeys.Read, serviceResponse.Data);
            }
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        private async Task<Pagination<TDomainModel>> NormalizePagination(Pagination<TDomainModel> pagination)
        {
            var p = pagination ??= new Pagination<TDomainModel>();

            if (!p.QueryOrFilter.HasValue() && p.QueryFunc == null)
                p.QueryFunc = x => x.Id.HasValue();

            var filter = p.QueryOrFilter.HasValue() ?
                await FilterFactory.GetFilter<TDomainModel>(p.QueryOrFilter) : null;

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
                    var right = ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>(p.QueryOrFilter)?.Compile();
                    if (right == null) return null;

                    var ecr = WorkContext.CurrentEntityConfigRecord;
                    if (Config.ManageEntityPermissions)
                    {
                        var permittedIds = await PermissionManager.GetPermittedIds(WorkContext.CurrentUserId, ecr.EntityKey, ecr.PermissionRecord.ReadKey);
                        Func<TDomainModel, bool> left = a => permittedIds.Contains(a.Id);
                        p.QueryFunc = x => left(x) && right(x);
                    }
                    else
                    {
                        p.QueryFunc = x => right(x);
                    }
                }
            }

            if (p.QueryFunc == null) return null;

            var paginationSettings = WorkContext.CurrentEntityConfigRecord.PaginationSettings;
            p.OrderBy ??= paginationSettings.DefaultOrderBy;
            p.Offset = p.Offset != 0 ? p.Offset : paginationSettings.DefaultOffset;
            p.PageSize = p.PageSize != 0 ? p.PageSize : paginationSettings.DefaultPageSize;
            p.IncludeNested = p.IncludeNested;
            p.SortOrder ??= paginationSettings.DefaultSortOrder;

            return p;
        }

        public virtual async Task<ServiceResponse> Update(string id, TDomainModel entity)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start update flow for id: {id}, entity: {entity}");
            entity.Id = id;
            var serviceResponse = new ServiceResponse();

            if (!await Validator.ValidateForUpdate(entity, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Repository - Fetch entity");
            var wrapper = new ServiceResponseWrapper(serviceResponse);

            var dbModel = await Repository.Query(async r => await r.GetById(id), wrapper);
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Update, id))
                return serviceResponse;

            var deletable = dbModel as IDeletableAudit;
            if (deletable != null && deletable.Deleted)
            {
                Logger.LogDebug(LoggingEvents.Audity, "entity already deleted");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }
            await ModelPreparar.PrepareForUpdate(dbModel, entity);

            Logger.LogDebug(LoggingEvents.Repository, $"Update entity in repository");
            var updateResponse = await Repository.Command(r => r.Update(entity), wrapper);
            Logger.LogDebug(LoggingEvents.Repository, $"Repository update response: {updateResponse}");
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Update, entity))
                return serviceResponse;

            if (entity is IFileContainer)
            {
                var fileContainer = (entity as IFileContainer);
                await FileStorageManager.Delete((dbModel as IFileContainer).Files);
                (updateResponse as IFileContainer).Files = fileContainer.Files;
                Logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start file uploads");
                await UploadFiles(fileContainer, serviceResponse);
            }

            Publish(EventKeys.Update, serviceResponse.Data);
            serviceResponse.Result = ServiceResult.Ok;
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public virtual async Task<ServiceResponse> Delete(string id)
        {
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start delete flow for id: {id}");
            var serviceResponse = new ServiceResponse();

            if (!await Validator.ValidateForDelete(id, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Entity did not pass validation");

            Logger.LogDebug(LoggingEvents.Repository, "Repository - Fetch entity");
            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var dbModel = await Repository.Query(r => r.GetById(id), wrapper);
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Delete, id))
                return serviceResponse;


            TDomainModel deletedModel;
            if (dbModel is IDeletableAudit)
            {
                Logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for deletion");

                await ModelPreparar.PrepareForDelete(dbModel);
                Logger.LogDebug(LoggingEvents.Repository, $"Repository - update {nameof(IDeletableAudit)} entity");
                deletedModel = await Repository.Command(r => r.Update(dbModel), wrapper);
            }
            else
            {
                Logger.LogDebug(LoggingEvents.Repository, "Repository - delete entity with id " + id);
                deletedModel = await Repository.Command(r => r.Delete(dbModel), wrapper);
            }

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, EventKeys.Delete, id))
                return serviceResponse;

            Publish(EventKeys.Delete, serviceResponse.Data);
            serviceResponse.Result = ServiceResult.Ok;
            Logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        #region Utilities
        private ServiceResponse SetServiceResponse(ServiceResponse serviceResponse, string serviceResponseResult, EventId eventId, string logMessage)
        {
            Logger.LogDebug(eventId, logMessage);
            serviceResponse.Result = serviceResponseResult;
            return serviceResponse;
        }
        private bool IsNotFoundOrBadOrMissingDataOrError(ServiceResponseWrapper wrapper, string eventKey, object data)
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
                PublishException(serviceResponse, eventKey, data, wrapper.Exception);
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
                PerformedByUserId = WorkContext.CurrentUserId
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
