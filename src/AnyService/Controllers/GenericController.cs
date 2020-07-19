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
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;
using Microsoft.Extensions.Logging;

namespace AnyService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [GenericControllerNameConvention]
    public class GenericController<TDomainModel> : ControllerBase where TDomainModel : IDomainModelBase
    {
        #region fields
        private readonly ICrudService<TDomainModel> _crudService;
        private readonly IServiceResponseMapper _serviceResponseMapper;
        private readonly ILogger<GenericController<TDomainModel>> _logger;
        private readonly AnyServiceConfig _config;
        private readonly Type _curType;
        private readonly string _curTypeName;
        private readonly WorkContext _workContext;
        private static readonly IDictionary<Type, PropertyInfo> FilesPropertyInfos = new Dictionary<Type, PropertyInfo>();
        #endregion
        #region ctor
        public GenericController(
            IServiceProvider serviceProvider, AnyServiceConfig config,
            IServiceResponseMapper serviceResponseMapper, WorkContext workContext,
            ILogger<GenericController<TDomainModel>> logger)
        {
            _crudService = serviceProvider.GetService<ICrudService<TDomainModel>>();
            _config = config;
            _serviceResponseMapper = serviceResponseMapper;
            _workContext = workContext;
            _logger = logger;
            _curType = typeof(TDomainModel);
            _curTypeName = _curType.Name;
        }
        #endregion
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TDomainModel model)
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Post flow");

            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });

            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Call service with value: " + model);
            var res = await _crudService.Create(model);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Post service response value: " + res);
            return _serviceResponseMapper.Map(res);
        }

        [HttpPost(Consts.MultipartSuffix)]
        public async Task<IActionResult> PostMultipart()
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Post for multipart flow");

            if (!Request.HasFormContentType) return BadRequest();
            var form = Request.Form;
            var model = form["model"].ToString().ToObject<TDomainModel>();

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

            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Call service with value: " + model);
            var res = await _crudService.Create(model);

            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Post service response value: " + res);
            return _serviceResponseMapper.Map(res);
        }

        [DisableFormValueModelBinding]
        [HttpPost(Consts.StreamSuffix)]
        public async Task<IActionResult> PostMultipartStream()
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Post for multipart flow stream");
            var model = await ExctractModelFromStream();
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Call service with value: " + model);
            var res = await _crudService.Create(model);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Post service response value: " + res);

            return _serviceResponseMapper.Map(res);
        }
        [DisableFormValueModelBinding]
        [HttpPut(Consts.StreamSuffix + "/{id}")]
        public async Task<IActionResult> PutMultipartStream()
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Put for multipart flow stream");
            var model = await ExctractModelFromStream();
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Call service with value: " + model);
            var res = await _crudService.Update(_workContext.RequestInfo.RequesteeId, model);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Put service response value: " + res);

            return _serviceResponseMapper.Map(res);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Get by id flow with id " + id);
            var res = await _crudService.GetById(id);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Get all service response value: " + res);
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int offset,
            [FromQuery] int pageSize,
            [FromQuery] string orderBy = null,
            [FromQuery] bool withNavProps = true,
            [FromQuery] string sortOrder = "desc",
            [FromQuery] string query = "")
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Get all flow. With values: " +
                $"\'{nameof(orderBy)}\' = \'{orderBy}\', \'{nameof(offset)}\' = \'{offset}\', \'{nameof(pageSize)}\' = \'{pageSize}\', " +
                $"\'{nameof(withNavProps)}\' = \'{withNavProps}\', \'{nameof(sortOrder)}\' = \'{sortOrder}\', \'{nameof(query)}\' = \'{query}\'");

            var pagination = GetPagination(orderBy, offset, pageSize, withNavProps, sortOrder, query);
            var srvRes = await _crudService.GetAll(pagination);
            _logger.LogDebug(LoggingEvents.Controller,
                $"Get all public service result: '{srvRes.Result}', message: '{srvRes.Message}', exceptionId: '{srvRes.ExceptionId}', data: '{pagination.Data.ToJsonString()}'");
            srvRes.Data = pagination?.Map<PaginationModel<TDomainModel>>();
            return _serviceResponseMapper.Map(srvRes);
        }

        private Pagination<TDomainModel> GetPagination(string orderBy, int offset, int pageSize, bool withNavProps, string sortOrder, string query)
        {
            return new Pagination<TDomainModel>
            {
                OrderBy = orderBy,
                Offset = offset,
                PageSize = pageSize,
                IncludeNested = withNavProps,
                SortOrder = sortOrder,
                QueryOrFilter = query
            };

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] TDomainModel model)
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Put flow");

            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });

            _logger.LogDebug($"{_curTypeName}: Start update flow with id {id} and model {model}");
            var res = await _crudService.Update(id, model);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Update service response value: " + res);
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Start Delete flow with id " + id);
            var res = await _crudService.Delete(id);
            _logger.LogDebug(LoggingEvents.Controller, $"{_curTypeName}: Delete service response value: " + res);
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
        #region Utilities
        private async Task<TDomainModel> ExctractModelFromStream()
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

            var model = modelJson.ToObject<TDomainModel>();
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