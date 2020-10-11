using AnyService.Conventions;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Controllers
{
    [GenericControllerModelConvention]
    public class GenericAggregateRootController<TModel, TDomainEntity> : ControllerBase
        where TDomainEntity : IDomainEntity
    {
        #region fields
        private static Type _entityType;
        private static Type _modelType;
        private static string _controllerName;

        private readonly ICrudService<TDomainEntity> _crudService;
        private readonly IServiceResponseMapper _serviceResponseMapper;
        private readonly ILogger<GenericAggregateRootController<TModel, TDomainEntity>> _logger;

        //private readonly ILogger<GenericParentController<T>> _logger;
        //private readonly AnyServiceConfig _config;
        //private readonly WorkContext _workContext;
        //private readonly Type _curType;
        //private readonly Type _mapToType;
        #endregion
        #region ctor
        public GenericAggregateRootController(
            ICrudService<TDomainEntity> crudService,
            IServiceResponseMapper serviceResponseMapper,
            ILogger<GenericAggregateRootController<TModel, TDomainEntity>> logger)
        {
            _crudService = crudService;
            _serviceResponseMapper = serviceResponseMapper;
            _logger = logger;
            //_config = config;
            //_serviceResponseMapper = serviceResponseMapper;
            //_workContext = workContext;
            //_logger = logger;
            _modelType ??= typeof(TModel);
            _entityType ??= typeof(TDomainEntity);
            _controllerName ??= nameof(GenericAggregateRootController<TModel, TDomainEntity>);
        }
        #endregion
        #region Create
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TModel model)
        {
            _logger.LogInformation(LoggingEvents.Controller,
                $"{nameof(GenericAggregateRootController<TModel, TDomainEntity>)}, model type: {_modelType.Name}, aggregate root type: {_entityType.Name}: Start Post flow");

            if (!ModelState.IsValid)
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });

            _logger.LogDebug(LoggingEvents.Controller, $"{_controllerName}: Call service with value: " + model?.ToJsonString());
            var entity = model.Map<TDomainEntity>();

            var res = await _crudService.Create(entity);
            _logger.LogDebug(LoggingEvents.Controller, $"{_controllerName}: Post service response value: " + res);
            return _serviceResponseMapper.MapServiceResponse(_entityType, _modelType, res);
        }
        #endregion
        #region GetById
        [Route("{id}")]
        public async Task<IActionResult> GetById(string id, string include)
        {
            throw new NotImplementedException();
            
        }

        private IEnumerable<string> GetAggregatedValuesToFetch(string aggregated)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}