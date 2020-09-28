using AnyService.Models;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace AnyService.Controllers
{

    [ApiController]
    [Route("[controller]")]
    [GenericControllerNameConvention]
    public class GenericParentController<T> : ControllerBase
    {
        #region fields
        //private readonly ICrudService<T> _crudService;
        //private readonly IServiceResponseMapper _serviceResponseMapper;
        //private readonly ILogger<GenericParentController<T>> _logger;
        //private readonly AnyServiceConfig _config;
        //private readonly WorkContext _workContext;
        //private readonly Type _curType;
        //private readonly Type _mapToType;
        #endregion
        #region ctor
        public GenericParentController(
            IServiceProvider serviceProvider, AnyServiceConfig config,
            IServiceResponseMapper serviceResponseMapper, WorkContext workContext,
            ILogger<GenericParentController<T>> logger)
        {
            //_crudService = serviceProvider.GetService<ICrudService<T>>();
            //_config = config;
            //_serviceResponseMapper = serviceResponseMapper;
            //_workContext = workContext;
            //_logger = logger;
        }
        #endregion
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] T model)
        {
            throw new NotImplementedException();
            //_logger.LogInformation(LoggingEvents.Controller, $"{_curTypeName}: Start Post flow");

            //if (!ModelState.IsValid || model.Equals(default))
            //    return new BadRequestObjectResult(new
            //    {
            //        message = "Bad or missing data",
            //        data = model
            //    });

            //_logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Call service with value: " + model);
            //var res = await _crudService.Create(model);
            //_logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Post service response value: " + res);

            //return MapServiceResponseIfRequired(res);

        }
    }
}
