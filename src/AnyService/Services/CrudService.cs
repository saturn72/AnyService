using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyService.Audity;
using AnyService.Core;
using AnyService.Events;
using AnyService.Services.FileStorage;
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
        private readonly IDomainEventsBus _eventBus;
        private readonly EventKeyRecord _eventKeyRecord;
        private readonly IFileStoreManager _fileStorageManager;
        private readonly ILogger<CrudService<TDomainModel>> _logger;
        #endregion
        #region ctor
        public CrudService(
            IRepository<TDomainModel> repository,
            ICrudValidator<TDomainModel> validator,
            AuditHelper auditHelper,
            WorkContext workContext,
            IDomainEventsBus eventBus,
            EventKeyRecord eventKeyRecord,
            IFileStoreManager fileStorageManager,
            ILogger<CrudService<TDomainModel>> logger)
        {
            _repository = repository;
            _validator = validator;
            _auditHelper = auditHelper;
            _workContext = workContext;
            _eventBus = eventBus;
            _eventKeyRecord = eventKeyRecord;
            _fileStorageManager = fileStorageManager;
            _logger = logger;
        }

        #endregion
        public async Task<ServiceResponse> Create(TDomainModel entity)
        {
            _logger.LogDebug($"Start create flow for entity: {entity}");

            var serviceResponse = new ServiceResponse();
            if (!await _validator.ValidateForCreate(entity, serviceResponse))
            {
                _logger.LogDebug("Entity did not pass validation");
                return serviceResponse;
            }

            if (entity is ICreatableAudit)
            {
                _logger.LogDebug("Audity - prepare for creation");
                _auditHelper.PrepareForCreate(entity as ICreatableAudit, _workContext.CurrentUserId);
            }

            _logger.LogDebug($"Insert entity to repository");
            var dbData = await _repository.Command(r => r.Insert(entity), serviceResponse);
            _logger.LogDebug($"Repository insert response: {dbData}");

            if (dbData == null)
            {
                _logger.LogDebug("Repository insert response is null - return back");
                return serviceResponse;
            }
            _logger.LogDebug($"Publish created event using {_eventKeyRecord.Create} key");

            _eventBus.Publish(_eventKeyRecord.Create, new DomainEventData
            {
                Data = dbData,
                PerformedByUserId = _workContext.CurrentUserId
            });
            serviceResponse.Result = ServiceResult.Ok;

            if (entity is IFileContainer)
            {
                _logger.LogDebug("Start file uploads");
                await UploadFiles(dbData as IFileContainer, serviceResponse);
            }
            _logger.LogDebug($"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public async Task UploadFiles(IFileContainer fileContainer, ServiceResponse serviceResponse)
        {
            var files = fileContainer.Files;
            if (!files.Any())
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
            _logger.LogDebug($"Start get by id with id = {id}");

            var serviceResponse = new ServiceResponse
            {
                Data = id
            };
            if (!await _validator.ValidateForGet(serviceResponse))
            {
                _logger.LogDebug("Entity did not pass validation");
                return serviceResponse;
            }

            _logger.LogDebug("Get by Id from repository");
            var data = await _repository.Query(r => r.GetById(id), serviceResponse);
            _logger.LogDebug($"Repository response: {data}");

            if (data != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Data = data;
                serviceResponse.Result = ServiceResult.Ok;

                _logger.LogDebug($"Publish Get event using {_eventKeyRecord.Read} key");
                _eventBus.Publish(_eventKeyRecord.Read, new DomainEventData
                {
                    Data = data,
                    PerformedByUserId = _workContext.CurrentUserId
                });
            }

            _logger.LogDebug($"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public async Task<ServiceResponse> GetAll()
        {
            _logger.LogDebug("Start get all flow");

            var serviceResponse = new ServiceResponse();
            if (!await _validator.ValidateForGet(serviceResponse))
            {
                _logger.LogDebug("Request did not pass validation");
                return serviceResponse;
            }

            var filter = _workContext.CurrentEntityConfigRecord.PublicGet ?
                null :
                new Dictionary<string, string> { { "CreatedByUserId", _workContext.CurrentUserId } };
            _logger.LogDebug("Get all filter = " + filter);
            _logger.LogDebug("Get all from repository");
            var data = await _repository.Query(r => r.GetAll(filter), serviceResponse) ?? new TDomainModel[] { };
            _logger.LogDebug($"Repository response: {data}");

            if (serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Data = data;
                serviceResponse.Result = ServiceResult.Ok;
                _logger.LogDebug($"Publish Get event using {_eventKeyRecord.Read} key");
                _eventBus.Publish(_eventKeyRecord.Read, new DomainEventData
                {
                    Data = data,
                    PerformedByUserId = _workContext.CurrentUserId
                });
            }
            _logger.LogDebug($"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public async Task<ServiceResponse> Update(string id, TDomainModel entity)
        {
            _logger.LogDebug($"Start update flow for id: {id}, entity: {entity}");
            entity.Id = id;
            var serviceResponse = new ServiceResponse();

            if (!await _validator.ValidateForUpdate(entity, serviceResponse))
            {
                _logger.LogDebug("Entity did not pass validation");
                return serviceResponse;
            }

            _logger.LogDebug("Repository - Fetch entity");
            var dbModel = await _repository.Query(async r => await r.GetById(id), serviceResponse);
            if (dbModel == null)
            {
                _logger.LogDebug("Repository response is null");
                return serviceResponse;
            }

            var deletable = dbModel as IDeletableAudit;
            if (deletable != null && deletable.Deleted)
            {
                _logger.LogDebug("entity already deleted");
                serviceResponse.Result = ServiceResult.BadOrMissingData;
                return serviceResponse;
            }

            if (entity is IUpdatableAudit)
            {
                _logger.LogDebug("Audity - prepare for update");
                _auditHelper.PrepareForUpdate(entity as IUpdatableAudit, dbModel as IUpdatableAudit, _workContext.CurrentUserId);
            }

            _logger.LogDebug($"Update entity in repository");
            var updateResponse = await _repository.Command(r => r.Update(entity), serviceResponse);
            _logger.LogDebug($"Repository update response: {updateResponse}");

            if (updateResponse != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                _logger.LogDebug($"Publish updated event using {_eventKeyRecord.Update} key");
                _eventBus.Publish(_eventKeyRecord.Update, new DomainEventData
                {
                    Data = updateResponse,
                    PerformedByUserId = _workContext.CurrentUserId
                });
                serviceResponse.Result = ServiceResult.Ok;
            }
            _logger.LogDebug($"Service Response: {serviceResponse}");
            return serviceResponse;
        }
        public async Task<ServiceResponse> Delete(string id)
        {
            _logger.LogDebug($"Start delete flow for id: {id}");
            var serviceResponse = new ServiceResponse();

            if (!await _validator.ValidateForDelete(id, serviceResponse))
            {
                _logger.LogDebug("Entity did not pass validation");
                return serviceResponse;
            }

            _logger.LogDebug("Repository - Fetch entity");
            var dbModel = await _repository.Query(r => r.GetById(id), serviceResponse);
            if (dbModel == null)
            {
                _logger.LogDebug("Repository response is null");
                return serviceResponse;
            }

            TDomainModel deletedModel;
            if (dbModel is IDeletableAudit)
            {
                _logger.LogDebug("Audity - prepare for deletion");

                _auditHelper.PrepareForDelete(dbModel as IDeletableAudit, _workContext.CurrentUserId);
                _logger.LogDebug("Repository - Update entity");
                deletedModel = await _repository.Command(r => r.Update(dbModel), serviceResponse);
                if (deletedModel == null)
                {
                    _logger.LogDebug("Repository - return null");
                    return serviceResponse;
                }
            }
            else
            {
                throw new System.NotImplementedException("need to implement delete logic in database");
            }

            _logger.LogDebug($"Publish updated event using {_eventKeyRecord.Delete} key");
            _eventBus.Publish(_eventKeyRecord.Delete, new DomainEventData
            {
                Data = deletedModel,
                PerformedByUserId = _workContext.CurrentUserId
            });
            serviceResponse.Result = ServiceResult.Ok;
            _logger.LogDebug($"Service Response: {serviceResponse}");
            return serviceResponse;
        }
    }
}
