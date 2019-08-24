using System.Threading.Tasks;
using AnyService.Audity;

namespace AnyService.Services
{
    public class CrudService<TDomainModel> where TDomainModel : IDomainModelBase
    {
        #region fields
        private readonly IRepository<TDomainModel> _repository;
        private readonly ICrudValidator<TDomainModel> _validator;
        private readonly AuditHelper _auditHelper;
        private readonly AnyServiceWorkContext _workContext;
        private readonly IEventBus _eventBus;
        private readonly EventKeyRecord _eventKeyRecord;
        #endregion
        #region ctor
        public CrudService(
            IRepository<TDomainModel> repository,
            ICrudValidator<TDomainModel> validator,
            AuditHelper auditHelper,
            AnyServiceWorkContext workContext,
            IEventBus eventBus,
            EventKeyRecord eventKeyRecord)
        {
            _repository = repository;
            _validator = validator;
            _auditHelper = auditHelper;
            _workContext = workContext;
            _eventBus = eventBus;
            _eventKeyRecord = eventKeyRecord;
        }

        #endregion

        public async Task<ServiceResponse> Create(TDomainModel entity)
        {
            var serviceResponse = new ServiceResponse();
            if (!await _validator.ValidateForCreate(entity, serviceResponse))
                return serviceResponse;

            if (entity is ICreatableAudit)
                _auditHelper.PrepareForCreate(entity as ICreatableAudit, _workContext.CurrentUserId);

            var dbData = await _repository.Command(r => r.Insert(entity), serviceResponse);

            if (dbData == null)
                return serviceResponse;

            serviceResponse.Result = ServiceResult.Ok;
            _eventBus.Publish(_eventKeyRecord.Create, new EventData
            {
                Data = dbData,
                CurrentUserId = _workContext.CurrentUserId
            });
            return serviceResponse;
        }
        public async Task<ServiceResponse> GetById(string id)
        {
            var serviceResponse = new ServiceResponse();
            serviceResponse.Data = id;
            if (!await _validator.ValidateForGet(serviceResponse))
                return serviceResponse;

            var data = await _repository.Query(r => r.GetById(id), serviceResponse);
            if (data != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Data = data;
                serviceResponse.Result = ServiceResult.Ok;
                _eventBus.Publish(_eventKeyRecord.Read, new EventData
                {
                    Data = new
                    {
                        Data = data,
                        CurrentUserId = _workContext.CurrentUserId
                    }
                });
            }
            return serviceResponse;
        }
        public async Task<ServiceResponse> GetAll()
        {
            var serviceResponse = new ServiceResponse();
            if (!await _validator.ValidateForGet(serviceResponse))
                return serviceResponse;
            var data = await _repository.Query(r => r.GetAll(), serviceResponse);
            if (data != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                serviceResponse.Data = data;
                serviceResponse.Result = ServiceResult.Ok;
                _eventBus.Publish(_eventKeyRecord.Read, new EventData
                {
                    Data = new
                    {
                        Data = data,
                        CurrentUserId = _workContext.CurrentUserId
                    }
                });
            }
            return serviceResponse;
        }
        public async Task<ServiceResponse> Update(string id, TDomainModel entity)
        {
            entity.Id = id;
            var serviceResponse = new ServiceResponse();

            if (!await _validator.ValidateForUpdate(entity, serviceResponse))
                return serviceResponse;

            var dbModel = await _repository.Query(async r => await r.GetById(id), serviceResponse);
            if (dbModel == null)
                return serviceResponse;
            serviceResponse.Result = ServiceResult.NotSet;

            if (entity is IUpdatableAudit)
                _auditHelper.PrepareForUpdate(entity as IUpdatableAudit, dbModel as IUpdatableAudit, _workContext.CurrentUserId);
            var updateResponse = await _repository.Command(r => r.Update(entity), serviceResponse);

            if (updateResponse != null && serviceResponse.Result == ServiceResult.NotSet)
            {
                _eventBus.Publish(_eventKeyRecord.Update, new EventData
                {
                    Data = new
                    {
                        Data = updateResponse,
                        CurrentUserId = _workContext.CurrentUserId
                    }
                });
                serviceResponse.Result = ServiceResult.Ok;
            }
            return serviceResponse;
        }
        public async Task<ServiceResponse> Delete(string id)
        {
            var serviceResponse = new ServiceResponse();

            if (!await _validator.ValidateForDelete(id, serviceResponse))
                return serviceResponse;

            var dbModel = await _repository.Query(r => r.GetById(id), serviceResponse);
            if (dbModel == null)
                return serviceResponse;

            TDomainModel deletedModel;
            if (dbModel is IDeletableAudit)
            {
                _auditHelper.PrepareForDelete(dbModel as IDeletableAudit, _workContext.CurrentUserId);
                deletedModel = await _repository.Command(r => r.Update(dbModel), serviceResponse);
                if (deletedModel == null)
                    return serviceResponse;
            }
            else
            {
                throw new System.NotImplementedException("need to implement delete logic in database");
            }

            _eventBus.Publish(_eventKeyRecord.Delete, new EventData
            {
                Data = new
                {
                    Data = deletedModel,
                    CurrentUserId = _workContext.CurrentUserId
                }
            });
            serviceResponse.Result = ServiceResult.Ok;
            return serviceResponse;
        }
    }
}
