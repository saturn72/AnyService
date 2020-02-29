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
using Microsoft.AspNetCore.Authorization;
using AnyService.Services;
using AnyService.Services.ServiceResponseMappers;

namespace AnyService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [GenericControllerNameConvention]
    [Authorize]
    public class GenericController<TDomainModel> : ControllerBase where TDomainModel : IDomainModelBase
    {
        #region fields
        private readonly CrudService<TDomainModel> _crudService;
        private readonly IServiceResponseMapper _serviceResponseMapper;
        private readonly AnyServiceConfig _config;
        private readonly Type _curType;
        private static readonly IDictionary<Type, PropertyInfo> FilesPropertyInfos = new Dictionary<Type, PropertyInfo>();
        #endregion
        #region ctor
        public GenericController(IServiceProvider serviceProvider, AnyServiceConfig config, IServiceResponseMapper serviceResponseMapper)
        {
            _crudService = serviceProvider.GetService<CrudService<TDomainModel>>();
            _config = config;
            _serviceResponseMapper = serviceResponseMapper;
            _curType = typeof(TDomainModel);
        }
        #endregion
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TDomainModel model)
        {
            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });


            var res = await _crudService.Create(model);
            return _serviceResponseMapper.Map(res);
        }

        [HttpPost(Consts.MultipartSuffix)]
        public async Task<IActionResult> PostMultipart()
        {
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

            var filesPropertyInfo = FilesPropertyInfos.TryGetValue(_curType, out PropertyInfo pi)
                ? pi
                : (pi = FilesPropertyInfos[_curType] = _curType.GetProperty(nameof(IFileContainer.Files)));
            filesPropertyInfo.SetValue(model, fileList);
            var res = await _crudService.Create(model);
            return _serviceResponseMapper.Map(res);
        }

        [DisableFormValueModelBinding]
        [HttpPost(Consts.StreamSuffix)]
        public async Task<IActionResult> PostMultipartStream()
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
            _curType.GetProperty(nameof(IFileContainer.Files)).SetValue(model, files);
            var res = await _crudService.Create(model);
            return _serviceResponseMapper.Map(res);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var res = await _crudService.GetById(id);
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var res = await _crudService.GetAll();
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] TDomainModel model)
        {
            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });

            var res = await _crudService.Update(id, model);
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var res = await _crudService.Delete(id);
            return _serviceResponseMapper.Map(res as ServiceResponse);
        }
    }
}