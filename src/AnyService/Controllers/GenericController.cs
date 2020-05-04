using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyService.Core;
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
using System.Linq.Expressions;

namespace AnyService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [GenericControllerNameConvention]
    public class GenericController<TDomainModel> : ControllerBase where TDomainModel : IDomainModelBase
    {
        #region fields
        private readonly CrudService<TDomainModel> _crudService;
        private readonly IServiceResponseMapper _serviceResponseMapper;
        private readonly ILogger<GenericController<TDomainModel>> _logger;
        private readonly AnyServiceConfig _config;
        private readonly Type _curType;
        private readonly WorkContext _workContext;
        private static readonly IDictionary<Type, PropertyInfo> FilesPropertyInfos = new Dictionary<Type, PropertyInfo>();
        private static readonly IDictionary<Type, IDictionary<string, string>> GetAllPublicFilterCollection = new Dictionary<Type, IDictionary<string, string>>();
        #endregion
        #region ctor
        public GenericController(
            IServiceProvider serviceProvider, AnyServiceConfig config,
            IServiceResponseMapper serviceResponseMapper, WorkContext workContext,
            ILogger<GenericController<TDomainModel>> logger)
        {
            _crudService = serviceProvider.GetService<CrudService<TDomainModel>>();
            _config = config;
            _serviceResponseMapper = serviceResponseMapper;
            _workContext = workContext;
            _logger = logger;
            _curType = typeof(TDomainModel);
        }
        #endregion
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TDomainModel model)
        {
            _logger.LogDebug(LoggingEvents.Controller, "Start Post flow");

            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });

            _logger.LogDebug(LoggingEvents.Controller, "Call service with value: " + model);
            var res = await _crudService.Create(model);
            _logger.LogDebug(LoggingEvents.Controller, "Post service response value: " + res);
            return _serviceResponseMapper.Map(res);
        }

        [HttpPost(Consts.MultipartSuffix)]
        public async Task<IActionResult> PostMultipart()
        {
            _logger.LogDebug(LoggingEvents.Controller, "Start Post for multipart flow");

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

            _logger.LogDebug(LoggingEvents.Controller, "Call service with value: " + model);
            var res = await _crudService.Create(model);

            _logger.LogDebug(LoggingEvents.Controller, "Post service response value: " + res);
            return _serviceResponseMapper.Map(res);
        }

        [DisableFormValueModelBinding]
        [HttpPost(Consts.StreamSuffix)]
        public async Task<IActionResult> PostMultipartStream()
        {
            _logger.LogDebug(LoggingEvents.Controller, "Start Post for multipart flow stream");
            var model = await ExctractModelFromStream();
            _logger.LogDebug(LoggingEvents.Controller, "Call service with value: " + model);
            var res = await _crudService.Create(model);
            _logger.LogDebug(LoggingEvents.Controller, "Post service response value: " + res);

            return _serviceResponseMapper.Map(res);
        }
        [DisableFormValueModelBinding]
        [HttpPut(Consts.StreamSuffix + "/{id}")]
        public async Task<IActionResult> PutMultipartStream()
        {
            _logger.LogDebug(LoggingEvents.Controller, "Start Put for multipart flow stream");
            var model = await ExctractModelFromStream();
            _logger.LogDebug(LoggingEvents.Controller, "Call service with value: " + model);
            var res = await _crudService.Update(_workContext.RequestInfo.RequesteeId, model);
            _logger.LogDebug(LoggingEvents.Controller, "Put service response value: " + res);

            return _serviceResponseMapper.Map(res);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            _logger.LogDebug(LoggingEvents.Controller, "Start Get by id flow with id " + id);
            var res = await _crudService.GetById(id);
            _logger.LogDebug(LoggingEvents.Controller, "Get all service response value: " + res);
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery]string orderBy = null,
            [FromQuery]ulong? offset = null,
            [FromQuery]ulong? pageSize = null,
            [FromQuery]string sortOrder = "desc",
            [FromQuery]string query = "")
        {
            var paginationSettings = _workContext.CurrentEntityConfigRecord.PaginationSettings;
            var pagination = GetPagination(orderBy, offset, pageSize, sortOrder, query);

            _logger.LogDebug(LoggingEvents.Controller, "Start Get all flow");
            var res = await _crudService.GetAll(pagination);
            _logger.LogDebug(LoggingEvents.Controller, "Get all public service response value: " + res);
            res.Data = pagination?.Map<PaginationApiModel<TDomainModel>>();
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }

        private Pagination<TDomainModel> GetPagination(string orderBy, ulong? offset, ulong? pageSize, string sortOrder, string query)
        {
            var curCfg = _workContext.CurrentEntityConfigRecord;
            Pagination<TDomainModel> pagination;

            if (curCfg.GetAllQueries.TryGetValue(query, out Func<object, Func<object, bool>> value))
            {
                var payload = new
                {
                    Type = typeof(TDomainModel),
                    UserId = _workContext.CurrentUserId,
                    Query = query
                };
                var func = value(payload).Convert<object, TDomainModel, bool>();
                Expression<Func<TDomainModel, bool>> objExp = x => func(x);
                pagination = new Pagination<TDomainModel>(objExp);
            }
            else
            {
                pagination = new Pagination<TDomainModel>(query);
            }

            var paginationSettings = curCfg.PaginationSettings;

            pagination.OrderBy = orderBy ?? paginationSettings.DefaultOrderBy;
            pagination.Offset = offset ?? paginationSettings.DefaultOffset;
            pagination.PageSize = pageSize ?? paginationSettings.DefaultPageSize;
            pagination.SortOrder = sortOrder ?? paginationSettings.DefaultSortOrder;

            return pagination;

        }

        [HttpGet(Consts.PublicSuffix)]
        public async Task<IActionResult> GetAllPublic()
        {
            var filter = GetAllPublicFilter();
            throw new NotImplementedException();
            // return await GetAllFiltered(filter);
        }

        private IDictionary<string, string> GetAllPublicFilter()
        {
            if (!GetAllPublicFilterCollection.TryGetValue(_curType, out IDictionary<string, string> filter))
            {
                filter = typeof(IPublishable).IsAssignableFrom(_curType) ?
                    new Dictionary<string, string> { { nameof(IPublishable.Public), "true" } } :
                    new Dictionary<string, string>();
                GetAllPublicFilterCollection[_curType] = filter;
            }
            return filter;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] TDomainModel model)
        {
            _logger.LogDebug(LoggingEvents.Controller, "Start Put flow");

            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });

            _logger.LogDebug($"Start update flow with id {id} and model {model}");
            var res = await _crudService.Update(id, model);
            _logger.LogDebug(LoggingEvents.Controller, "Update service response value: " + res);
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogDebug(LoggingEvents.Controller, "Start Delete flow with id " + id);
            var res = await _crudService.Delete(id);
            _logger.LogDebug(LoggingEvents.Controller, "Delete service response value: " + res);
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