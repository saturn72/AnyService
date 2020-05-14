using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Core;
using AnyService.Core.Security;
using AnyService.Events;
using AnyService.Services.FileStorage;
using AnyService.Utilities;
using Microsoft.Extensions.Logging;

namespace AnyService.Services
{
    public class CrudService<TDomainModel> where TDomainModel : IDomainModelBase
    {
        #region fields
        private readonly IRepository<TDomainModel> _repository;
        private readonly ICrudValidator<TDomainModel> _validator;
        private readonly AuditHelper _auditHelper;
        private readonly WorkContext _workContext;
        private readonly IEventBus _eventBus;
        private readonly EventKeyRecord _eventKeys;
        private readonly IFileStoreManager _fileStorageManager;
        private readonly ILogger<CrudService<TDomainModel>> _logger;
        private readonly IIdGenerator _idGenerator;
        private readonly IFilterFactory _filterFactory;
        private readonly IPermissionManager _permissionManager;
        #endregion
        #region ctor
        public CrudService(
            IRepository<TDomainModel> repository,
            ICrudValidator<TDomainModel> validator,
            AuditHelper auditHelper,
            WorkContext workContext,
            IEventBus eventBus,
            IFileStoreManager fileStorageManager,
            ILogger<CrudService<TDomainModel>> logger,
            IIdGenerator idGenerator,
            IFilterFactory filterFactory,
            IPermissionManager permissionManager)
        {
            _repository = repository;
            _validator = validator;
            _auditHelper = auditHelper;
            _workContext = workContext;
            _eventBus = eventBus;
            _eventKeys = workContext?.CurrentEntityConfigRecord?.EventKeys;
            _fileStorageManager = fileStorageManager;
            _logger = logger;
            _idGenerator = idGenerator;
            _filterFactory = filterFactory;
            _permissionManager = permissionManager;
        }

        #endregion
        public async Task<ServiceResponse> Create(TDomainModel entity)
        {
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start create flow for entity: {entity}");

            var serviceResponse = new ServiceResponse();
            if (!await _validator.ValidateForCreate(entity, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Entity did not pass validation");

            if (entity is ICreatableAudit)
            {
                _logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for creation");
                _auditHelper.PrepareForCreate(entity as ICreatableAudit, _workContext.CurrentUserId);
            }

            _logger.LogDebug(LoggingEvents.Repository, $"Insert entity to repository");

            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var dbData = await _repository.Command(r => r.Insert(entity), wrapper);
            _logger.LogDebug(LoggingEvents.Repository, $"Repository insert response: {dbData}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, _eventKeys.Create, entity))
                return serviceResponse;

            Publish(_eventKeys.Create, serviceResponse.Data);
            serviceResponse.Result = ServiceResult.Ok;

            if (entity is IFileContainer)
            {
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start file uploads");
                await UploadFiles(dbData as IFileContainer, serviceResponse);
            }
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        public async Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse)
        {
            var files = fileContainer?.Files;
            if (files == null || !files.Any())
                return;

            foreach (var f in files)
            {
                f.ParentId = fileContainer.Id;
                f.ParentKey = _workContext.CurrentEntityConfigRecord.EntityKey;
            }
            var uploadResponses = await _fileStorageManager.Upload(files);
            serviceResponse.Data = new { entity = serviceResponse.Data, filesUploadStatus = uploadResponses };
        }
        public async Task<ServiceResponse> GetById(string id)
        {
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start get by id with id = {id}");

            var serviceResponse = new ServiceResponse
            {
                Data = id
            };
            if (!await _validator.ValidateForGet(serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Entity did not pass validation");

            _logger.LogDebug(LoggingEvents.Repository, "Get by Id from repository");

            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var data = await _repository.Query(r => r.GetById(id), wrapper);
            _logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, _eventKeys.Read, id))
                return serviceResponse;

            if (data != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Data = data;
                serviceResponse.Result = ServiceResult.Ok;
                Publish(_eventKeys.Read, serviceResponse.Data);
            }
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public async Task<ServiceResponse> GetAll(Pagination<TDomainModel> pagination)
        {
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start get all flow");
            var serviceResponse = new ServiceResponse { Data = pagination };

            if (!await _validator.ValidateForGet(serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Request did not pass validation");

            if (!await NormalizePagination(pagination))
                return SetServiceResponse(serviceResponse, ServiceResult.BadOrMissingData, LoggingEvents.BusinessLogicFlow, "Missing query data");

            _logger.LogDebug(LoggingEvents.Repository, "Get all from repository using paginate = " + pagination);
            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var data = await _repository.Query(r => r.GetAll(pagination), wrapper);
            _logger.LogDebug(LoggingEvents.Repository, $"Repository response: {data}");

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, _eventKeys.Read, pagination))
                return serviceResponse;
            pagination.Data = data ?? new TDomainModel[] { };
            if (serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Data = pagination;
                serviceResponse.Result = ServiceResult.Ok;
                Publish(_eventKeys.Read, serviceResponse.Data);
            }
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        private async Task<bool> NormalizePagination(Pagination<TDomainModel> pagination)
        {
            if (pagination == null || !pagination.QueryAsString.HasValue()) return false;

            var filter = await _filterFactory.GetFilter<TDomainModel>(pagination.QueryAsString);

            if (filter != null)
            {
                var f = filter(new object());
                if (f == null)
                    return false;
                pagination.QueryFunc = c => f(c);
            }
            else
            {
                var right = ExpressionTreeBuilder.BuildBinaryTreeExpression<TDomainModel>(pagination.QueryAsString)?.Compile();
                if (right == null) return false;
                var ecr = _workContext.CurrentEntityConfigRecord;
                var permittedIds = await _permissionManager.GetPermittedIds(_workContext.CurrentUserId, ecr.EntityKey, ecr.PermissionRecord.ReadKey);
                Func<TDomainModel, bool> left = a => permittedIds.Contains(a.Id);
                pagination.QueryFunc = x => left(x) && right(x);
            }

            if (pagination.QueryFunc == null) return false;

            var paginationSettings = _workContext.CurrentEntityConfigRecord.PaginationSettings;
            pagination.OrderBy = pagination.OrderBy ?? paginationSettings.DefaultOrderBy;
            pagination.Offset = pagination.Offset ?? paginationSettings.DefaultOffset;
            pagination.PageSize = pagination.PageSize ?? paginationSettings.DefaultPageSize;
            pagination.IncludeNested = pagination.IncludeNested;
            pagination.SortOrder = pagination.SortOrder ?? paginationSettings.DefaultSortOrder;

            return true;
        }

        public async Task<ServiceResponse> Update(string id, TDomainModel entity)
        {
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start update flow for id: {id}, entity: {entity}");
            entity.Id = id;
            var serviceResponse = new ServiceResponse();

            if (!await _validator.ValidateForUpdate(entity, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Entity did not pass validation");

            _logger.LogDebug(LoggingEvents.Repository, "Repository - Fetch entity");
            var wrapper = new ServiceResponseWrapper(serviceResponse);

            var dbModel = await _repository.Query(async r => await r.GetById(id), wrapper);
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, _eventKeys.Update, id))
                return serviceResponse;

            var deletable = dbModel as IDeletableAudit;
            if (deletable != null && deletable.Deleted)
            {
                _logger.LogDebug(LoggingEvents.Audity, "entity already deleted");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            if (entity is IUpdatableAudit)
            {
                _logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for update");
                _auditHelper.PrepareForUpdate(entity as IUpdatableAudit, dbModel as IUpdatableAudit, _workContext.CurrentUserId);
            }

            _logger.LogDebug(LoggingEvents.Repository, $"Update entity in repository");
            var updateResponse = await _repository.Command(r => r.Update(entity), wrapper);
            _logger.LogDebug(LoggingEvents.Repository, $"Repository update response: {updateResponse}");
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, _eventKeys.Update, entity))
                return serviceResponse;

            if (entity is IFileContainer)
            {
                var fileContainer = (entity as IFileContainer);
                await _fileStorageManager.Delete((dbModel as IFileContainer).Files);
                (updateResponse as IFileContainer).Files = fileContainer.Files;
                _logger.LogDebug(LoggingEvents.BusinessLogicFlow, "Start file uploads");
                await UploadFiles(fileContainer, serviceResponse);
            }

            Publish(_eventKeys.Update, serviceResponse.Data);
            serviceResponse.Result = ServiceResult.Ok;
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public async Task<ServiceResponse> Delete(string id)
        {
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Start delete flow for id: {id}");
            var serviceResponse = new ServiceResponse();

            if (!await _validator.ValidateForDelete(id, serviceResponse))
                return SetServiceResponse(serviceResponse, ServiceResult.Unauthorized, LoggingEvents.Validation, "Entity did not pass validation");

            _logger.LogDebug(LoggingEvents.Repository, "Repository - Fetch entity");
            var wrapper = new ServiceResponseWrapper(serviceResponse);
            var dbModel = await _repository.Query(r => r.GetById(id), wrapper);
            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, _eventKeys.Delete, id))
                return serviceResponse;


            TDomainModel deletedModel;
            if (dbModel is IDeletableAudit)
            {
                _logger.LogDebug(LoggingEvents.Audity, "Audity - prepare for deletion");

                _auditHelper.PrepareForDelete(dbModel as IDeletableAudit, _workContext.CurrentUserId);
                _logger.LogDebug(LoggingEvents.Repository, $"Repository - update {nameof(IDeletableAudit)} entity");
                deletedModel = await _repository.Command(r => r.Update(dbModel), wrapper);
            }
            else
            {
                _logger.LogDebug(LoggingEvents.Repository, "Repository - delete entity with id " + id);
                deletedModel = await _repository.Command(r => r.Delete(dbModel), wrapper);
            }

            if (IsNotFoundOrBadOrMissingDataOrError(wrapper, _eventKeys.Delete, id))
                return serviceResponse;

            Publish(_eventKeys.Delete, serviceResponse.Data);
            serviceResponse.Result = ServiceResult.Ok;
            _logger.LogDebug(LoggingEvents.BusinessLogicFlow, $"Service Response: {serviceResponse}");
            return serviceResponse;
        }

        #region Utilities
        private ServiceResponse SetServiceResponse(ServiceResponse serviceResponse, string serviceResponseResult, EventId eventId, string logMessage)
        {
            _logger.LogDebug(eventId, logMessage);
            serviceResponse.Result = serviceResponseResult;
            return serviceResponse;
        }
        private bool IsNotFoundOrBadOrMissingDataOrError(ServiceResponseWrapper wrapper, string eventKey, object data)
        {
            var serviceResponse = wrapper.ServiceResponse;
            _logger.LogDebug(LoggingEvents.Repository, $"Has {serviceResponse.Result} response");
            if (serviceResponse.Result == ServiceResult.BadOrMissingData || serviceResponse.Result == ServiceResult.NotFound)
            {
                return true;
            }
            if (wrapper.Exception != null || serviceResponse.Result == ServiceResult.Error)
            {
                _logger.LogDebug(LoggingEvents.Repository, "Repository response is null");
                PublishException(serviceResponse, eventKey, data, wrapper.Exception);
                return true;
            }
            return false;
        }

        private void Publish(string eventKey, object data)
        {
            _logger.LogDebug(LoggingEvents.EventPublishing, $"Publish event using {eventKey} key");
            _eventBus.Publish(eventKey, new DomainEventData
            {
                Data = data,
                PerformedByUserId = _workContext.CurrentUserId
            });
        }
        private void PublishException(ServiceResponse serviceResponse, string eventKey, object data, Exception exception)
        {
            serviceResponse.ExceptionId = _idGenerator.GetNext();
            _logger.LogDebug(LoggingEvents.Repository, $"Repository returned with exception. exceptionId: {serviceResponse.ExceptionId}");

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
