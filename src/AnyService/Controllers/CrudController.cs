using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.Services.FileStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace AnyService.Controllers
{
    [Route(Consts.AnyServiceControllerName)]
    [ApiController]
    public class CrudController : ControllerBase
    {
        #region fields
        private readonly dynamic _crudService;
        private readonly WorkContext _workContext;
        private readonly AnyServiceConfig _config;
        private static MethodInfo CreateMethodInfo;
        private static readonly MethodInfo UpdateMethodInfo;
        private static IDictionary<Type, PropertyInfo> FilesPropertyInfos = new Dictionary<Type, PropertyInfo>();
        #endregion
        #region ctor
        public CrudController(dynamic crudService, WorkContext workContext, AnyServiceConfig config)
        {
            _crudService = crudService;
            _workContext = workContext;
            _config = config;
        }
        #endregion

        [HttpPost("{entityName}")]
        public async Task<IActionResult> Post([FromBody] JsonElement model)
        {
            if (!ModelState.IsValid || model.Equals(default))
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });
            var o = new JsonSerializerOptions
            {
                AllowTrailingCommas = true
            };


            var m = JsonSerializer.Deserialize(model.ToString(), _workContext.CurrentType, o);
            
            throw new NotImplementedException();
            var typedModel = model.ToObject(_workContext.CurrentType);
            return await Create(typedModel);
        }

        [HttpPost(Consts.MultipartPrefix + "/{entityName}")]
        public async Task<IActionResult> PostMultipart()
        {
            if (!Request.HasFormContentType) return BadRequest();
            var curType = _workContext.CurrentType;
            var form = Request.Form;
            throw new NotImplementedException();
            // var typedModel = JsonConvert.DeserializeObject(form["model"], curType);

            //var fileList = new List<FileModel>();
            //foreach (var ff in form.Files.Where(f => f.Length > 0))
            //{
            //    var fileModel = new FileModel
            //    {
            //        FileName = ff.FileName,
            //        ParentKey = curType.FullName,
            //    };
            //    using (var memoryStream = new MemoryStream())
            //    {
            //        await ff.CopyToAsync(memoryStream);
            //        fileModel.Bytes = memoryStream.ToArray();
            //    }
            //    fileList.Add(fileModel);
            //}

            //var filesPropertyInfo = FilesPropertyInfos.TryGetValue(curType, out PropertyInfo pi)
            //    ? pi
            //    : (pi = FilesPropertyInfos[curType] = curType.GetProperty(nameof(IFileContainer.Files)));
            //filesPropertyInfo.SetValue(typedModel, fileList);
            //return await Create(typedModel);
        }

        [DisableFormValueModelBinding]
        [HttpPost(Consts.MultipartPrefix + "/{entityName}" + "/" + Consts.StreamSuffix)]
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
                        var curFile = new FileModel();
                        curFile.TempPath = Path.GetTempFileName();
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
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            formAccumulator.Append(key.Value, value);

                            if (formAccumulator.ValueCount > _config.MaxValueCount)
                            {
                                throw new InvalidDataException($"Form key count limit {_config.MaxValueCount} exceeded.");
                            }
                        }
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }
            var modelJson = formAccumulator.GetResults()["model"].ToString();

            throw new NotImplementedException();
            //var model = JsonConvert.DeserializeObject(modelJson, _workContext.CurrentType);
            //_workContext.CurrentType.GetProperty(nameof(IFileContainer.Files)).SetValue(model, files);
            //return await Create(model);
        }

        [HttpGet("{entityName}/{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var res = await _crudService.GetById(id);
            return (res as ServiceResponse).ToActionResult();
        }
        [HttpGet("{entityName}")]
        public async Task<IActionResult> GetAll()
        {
            var res = await _crudService.GetAll();
            return (res as ServiceResponse).ToActionResult();
        }
        [HttpPut("{entityName}/{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] object model)
        {
            if (!ModelState.IsValid || model == null)
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });
            throw new NotImplementedException();
            //var typedModel = model.ToObject(_workContext.CurrentType);
            //var umi = UpdateMethodInfo ?? (UpdateMethodInfo = _crudService.GetType().GetMethod(nameof(CrudService<IDomainModelBase>.Update)));
            //var res = await umi.Invoke(_crudService, new[] { id, typedModel });
            //return (res as ServiceResponse).ToActionResult();
        }
        [HttpDelete("{entityName}/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var res = await _crudService.Delete(id);
            return (res as ServiceResponse).ToActionResult();
        }

        #region Utilities
        private async Task<IActionResult> Create(object model)
        {
            var cmi = CreateMethodInfo ?? (CreateMethodInfo = _crudService.GetType().GetMethod(nameof(CrudService<IDomainModelBase>.Create)));
            var res = await cmi.Invoke(_crudService, new[] { model });
            return (res as ServiceResponse).ToActionResult();
        }
        #endregion
    }
}