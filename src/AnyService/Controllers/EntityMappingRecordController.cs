using AnyService.Models;
using AnyService.Services;
using AnyService.Services.EntityMapping;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnyService.Controllers
{
    [ApiController]
    [Route("__map")]
    public class EntityMappingRecordController : ControllerBase
    {
        #region Fields
        private readonly IEntityMappingRecordManager _manager;
        private readonly IServiceResponseMapper _responseMapper;
        private readonly ILogger<EntityMappingRecordController> _logger;
        private static IReadOnlyDictionary<string, string> EntityExternalNames;
        #endregion
        #region ctor
        public EntityMappingRecordController(
            IEntityMappingRecordManager manager,
            IEnumerable<EntityConfigRecord> entityConfigRecords,
            IServiceResponseMapper responseMapper,
            ILogger<EntityMappingRecordController> logger)
        {
            _manager = manager;
            _responseMapper = responseMapper;
            _logger = logger;
            if (EntityExternalNames == null)
            {
                var names = new ConcurrentDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var ecr in entityConfigRecords)
                    names.TryAdd(ecr.ExternalName, ecr.Name);
                EntityExternalNames = names;
            }
        }
        #endregion
        [HttpPut("{parentId}")]
        public async Task<IActionResult> UpdateEntityMappings(string id, [FromBody] EntityMappingRequestModel model)
        {
            _logger.LogInformation(LoggingEvents.Controller, $"Start {nameof(UpdateEntityMappings)} Flow with parameters: {nameof(id)} = {id}, request = {model?.ToJsonString()}");
            var request = new EntityMappingRequest
            {
                ParentEntityKey = EntityExternalNames.GetValueOrDefault(model.ParentEntityKey),
                ParentId = id,
                ChildEntityKey = EntityExternalNames.GetValueOrDefault(model.ChildEntityKey),
                Add = model.Add,
                Remove = model.Remove
            };
            if (!ModelState.IsValid ||
                !request.ParentEntityKey.HasValue() ||
                !request.ChildEntityKey.HasValue())
                return BadRequest();

            _logger.LogDebug(LoggingEvents.Controller, $"Call mapping service with value: {request.ToJsonString()}");
            var res = await _manager.UpdateMapping(request);
            return _responseMapper.MapServiceResponse(res);
        }
    }
}
