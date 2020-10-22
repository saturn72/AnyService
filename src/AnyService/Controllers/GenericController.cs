using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyService.Services.FileStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.Extensions.Logging;
using AnyService.Conventions;

namespace AnyService.Controllers
{
    [GenericControllerModelConvention]
    public class GenericController<TModel, TDomainEntity> : ControllerBase
        where TModel : class
        where TDomainEntity : IEntity
    {
        #region fields

        private static Type _curType;
        private static Type _curEnumerableType;
        private static Type _mapToType;
        private static Type _mapToTypeEnumerableType;
        private static Type _mapToPageType;
        private static IEnumerable<string> _aggregatedChildNames;
        private static string _curEndpointName;

        private readonly ICrudService<TDomainEntity> _crudService;
        private readonly IServiceResponseMapper _serviceResponseMapper;
        private readonly ILogger<GenericController<TModel, TDomainEntity>> _logger;
        private readonly AnyServiceConfig _config;
        private readonly WorkContext _workContext;
        private static readonly IDictionary<Type, PropertyInfo> FilesPropertyInfos = new Dictionary<Type, PropertyInfo>();
        #endregion
        #region ctor
        public GenericController(
            ICrudService<TDomainEntity> crudService,
            AnyServiceConfig config,
            IServiceResponseMapper serviceResponseMapper,
            WorkContext workContext,
            ILogger<GenericController<TModel, TDomainEntity>> logger)
        {
            _crudService = crudService;
            _config = config;
            _serviceResponseMapper = serviceResponseMapper;
            _workContext = workContext;
            _logger = logger;

            if (_curEndpointName == null)
                InitStaticMembers();
        }
        #endregion
        #region POST
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody] TModel model)
        {
            _logger.LogInformation(LoggingEvents.Controller, $"{_curEndpointName}: Start Post flow");

            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });

            var entity = model.Map<TDomainEntity>();
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Call service with value: " + model);
            var res = await _crudService.Create(entity);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Post service response value: " + res);

            return _serviceResponseMapper.MapServiceResponse(_curType, _mapToType, res);
        }

        [HttpPost(Consts.MultipartSuffix)]
        public async Task<IActionResult> PostMultipart()
        {
            _logger.LogInformation(LoggingEvents.Controller, $"{_curEndpointName}: Start Post for multipart flow");

            if (!Request.HasFormContentType) return BadRequest();
            var form = Request.Form;
            var model = form["model"].ToString().ToObject<TDomainEntity>();

            var fileList = new List<FileModel>();
            foreach (var ff in form.Files.Where(f => f.Length > 0))
            {
                var fileModel = new FileModel
                {
                    UntrustedFileName = Path.GetFileName(ff.FileName),
                    DisplayFileName = WebUtility.HtmlEncode(ff.FileName),
                    StoredFileName = Path.GetRandomFileName(),
                };
                using (var memoryStream = new MemoryStream())
                {
                    await ff.CopyToAsync(memoryStream);
                    fileModel.Bytes = memoryStream.ToArray();
                }
                fileList.Add(fileModel);
            }

            var filesPropertyInfo = GetFilesProperty(_curType);
            filesPropertyInfo.SetValue(model, fileList);

            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Call service with value: " + model);
            var res = await _crudService.Create(model);

            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Post service response value: " + res);
            return _serviceResponseMapper.MapServiceResponse(_curType, _mapToType, res);
        }
        [DisableFormValueModelBinding]
        [HttpPost(Consts.StreamSuffix)]
        public async Task<IActionResult> PostMultipartStream()
        {
            _logger.LogInformation(LoggingEvents.Controller, $"{_curEndpointName}: Start Post for multipart flow stream");
            var model = await ExctractModelFromStream();
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Call service with value: " + model);
            var res = await _crudService.Create(model);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Post service response value: " + res);

            return _serviceResponseMapper.MapServiceResponse(_curType, _mapToType, res);
        }
        #endregion
        #region GET
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            string id,
            [FromQuery] string childNames = null
            )
        {
            _logger.LogInformation(LoggingEvents.Controller, $"{_curEndpointName}: Start Get by id flow with id " + id);

            ServiceResponse res = null;

            if (childNames.HasValue())
            {
                var aggChildNames = parseChildNames();
                if (aggChildNames.IsNullOrEmpty() || aggChildNames.All(_aggregatedChildNames.Contains))
                    return BadRequest();
                res = await _crudService.GetAggregated(id, aggChildNames);
            }
            else
            {
                res = await _crudService.GetById(id);
            }
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Get all service response value: " + res);

            if (res.Result == ServiceResult.NotFound) res.Result = ServiceResult.BadOrMissingData;
            return _serviceResponseMapper.MapServiceResponse(_curType, _mapToType, res);

            IEnumerable<string> parseChildNames() => childNames.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim());
        }


        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int offset,
            [FromQuery] int pageSize,
            [FromQuery] string orderBy = null,
            [FromQuery] bool withNavProps = true,
            [FromQuery] string sortOrder = "desc",
            [FromQuery] string query = "",
            [FromQuery] bool dataOnly = true
            )
        {
            _logger.LogInformation(LoggingEvents.Controller,
                $"{_curEndpointName}: Start Get all flow. With values: " +
                $"\'{nameof(offset)}\' = \'{offset}\', " +
                $"\'{nameof(pageSize)}\' = \'{pageSize}\', " +
                $"\'{nameof(orderBy)}\' = \'{orderBy}\', " +
                $"\'{nameof(withNavProps)}\' = \'{withNavProps}\', " +
                $"\'{nameof(sortOrder)}\' = \'{sortOrder}\', " +
                $"'{nameof(query)}\' = \'{query}\' " +
                $"\'{nameof(dataOnly)}\' = \'{dataOnly}\', "
                );

            var pagination = GetPagination(orderBy, offset, pageSize, withNavProps, sortOrder, query);
            var srvRes = await _crudService.GetAll(pagination);
            _logger.LogDebug(LoggingEvents.Controller,
                $"Get all public service result: '{srvRes.Result}', message: '{srvRes.Message}', exceptionId: '{srvRes.ExceptionId}', data: '{pagination.Data.ToJsonString()}'");
            if (!dataOnly)
                return _serviceResponseMapper.MapServiceResponse(typeof(Pagination<TDomainEntity>), _mapToPageType, srvRes);

            srvRes.PayloadObject = srvRes.Payload.Data;
            return _serviceResponseMapper.MapServiceResponse(_curEnumerableType, _mapToTypeEnumerableType, srvRes);
        }
        private Pagination<TDomainEntity> GetPagination(string orderBy, int offset, int pageSize, bool withNavProps, string sortOrder, string query)
        {
            return new Pagination<TDomainEntity>
            {
                OrderBy = orderBy,
                Offset = offset,
                PageSize = pageSize,
                IncludeNested = withNavProps,
                SortOrder = sortOrder,
                QueryOrFilter = query
            };
        }
        #endregion
        #region update
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] TModel model)
        {
            _logger.LogInformation(LoggingEvents.Controller, $"{_curEndpointName}: Start Put flow");

            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });

            var entity = model.Map<TDomainEntity>();
            _logger.LogDebug($"{_curEndpointName}: Start update flow with id {id} and model {model}");
            var res = await _crudService.Update(id, entity);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Update service response value: " + res);

            return _serviceResponseMapper.MapServiceResponse(_curType, _mapToType, res);
        }
        [DisableFormValueModelBinding]
        [HttpPut(Consts.StreamSuffix + "/{id}")]
        public async Task<IActionResult> PutMultipartStream()
        {
            _logger.LogInformation(LoggingEvents.Controller, $"{_curEndpointName}: Start Put for multipart flow stream");
            var model = await ExctractModelFromStream();
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Call service with value: " + model);
            var res = await _crudService.Update(_workContext.RequestInfo.RequesteeId, model);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Put service response value: " + res);

            return _serviceResponseMapper.MapServiceResponse(_curType, _mapToType, res);
        }
        [HttpPut("__map/{id}")]
        public async Task<IActionResult> UpdateEntityMappings(string id, [FromBody] EntityMappingRequest request)
        {
            _logger.LogInformation(LoggingEvents.Controller, $"Start {nameof(UpdateEntityMappings)} Flow with parameters: {nameof(id)} = {id}, request = {request?.ToJsonString()}");
            if (!ModelState.IsValid)
                return BadRequest();

            request.ParentId = id;
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Call service with value: " + request);

            var srvRes = await _crudService.UpdateMappings(request);
            return _serviceResponseMapper.MapServiceResponse(srvRes);

        }
        #endregion
        #region DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation(LoggingEvents.Controller, $"{_curEndpointName}: Start Delete flow with id " + id);
            var res = await _crudService.Delete(id);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curEndpointName}: Delete service response value: " + res);

            return _serviceResponseMapper.MapServiceResponse(_curType, _mapToType, res);
        }
        #endregion
        #region Utilities
        private void InitStaticMembers()
        {
            var es = _workContext.CurrentEndpointSettings;
            _curEndpointName ??= es.Name;
            _curType ??= es.EntityConfigRecord.Type;
            _curEnumerableType ??= typeof(IEnumerable<>).MakeGenericType(_curType);
            _mapToType ??= es?.MapToType;
            _mapToTypeEnumerableType ??= typeof(IEnumerable<>).MakeGenericType(_mapToType);
            _mapToPageType ??= es.MapToPaginationType;
            _aggregatedChildNames ??= es.AggregationData?.Keys;
        }
        private async Task<TDomainEntity> ExctractModelFromStream()
        {
            // Used to accumulate all the form url encoded key value pairs in the 
            // request.
            var formAccumulator = new KeyValueAccumulator();
            var files = new List<FileModel>();

            var contentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            var boundary = MultipartRequestHelper.GetBoundary(contentType, _config.MaxMultipartBoundaryLength);
            var reader = new MultipartReader(boundary.Value, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        var curFile = new FileModel
                        {
                            TempPath = Path.GetRandomFileName(),
                        };
                        using (var targetStream = System.IO.File.Create(curFile.TempPath))
                        {
                            await section.Body.CopyToAsync(targetStream);
                        }
                        files.Add(curFile);
                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        // Content-Disposition: form-data; name="key"
                        //
                        // value

                        // Do not limit the key name length here because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = MultipartRequestHelper.GetEncoding(section);
                        using var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true);
                        // The value length limit is enforced by MultipartBodyLengthLimit
                        var value = await streamReader.ReadToEndAsync();
                        if (string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                        {
                            value = string.Empty;
                        }
                        formAccumulator.Append(key.Value, value);

                        if (formAccumulator.ValueCount > _config.MaxValueCount)
                        {
                            throw new InvalidDataException($"Form key count limit {_config.MaxValueCount} exceeded.");
                        }
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }
            var modelJson = formAccumulator.GetResults()["model"].ToString();

            var model = modelJson.ToObject<TDomainEntity>();
            GetFilesProperty(_curType).SetValue(model, files);

            return model;
        }
        private static PropertyInfo GetFilesProperty(Type type)
        {
            if (!FilesPropertyInfos.TryGetValue(type, out PropertyInfo value))
            {
                value = type.GetProperty(nameof(IFileContainer.Files));
                FilesPropertyInfos[type] = value;
            }
            return value;
        }
        #endregion
    }
}