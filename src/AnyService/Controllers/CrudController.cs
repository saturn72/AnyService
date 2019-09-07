using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyService.Services;
using AnyService.Services.FileStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private static MethodInfo UpdateMethodInfo;
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
        public async Task<IActionResult> Post([FromBody] JObject model)
        {
            if (!ModelState.IsValid || model == null)
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });
            var typedModel = model.ToObject(_workContext.CurrentType);
            return await Create(typedModel);
        }

        [HttpPost(Consts.MultipartPrefix + "/{entityName}")]
        public async Task<IActionResult> PostMultipart()
        {
            if (!Request.HasFormContentType) return BadRequest();
            var curType = _workContext.CurrentType;
            var form = Request.Form;
            var typedModel = JsonConvert.DeserializeObject(form["model"], curType);

            var fileList = new List<FileModel>();
            foreach (var ff in form.Files.Where(f => f.Length > 0))
            {
                var fileModel = new FileModel
                {
                    FileName = ff.FileName,
                    ParentKey = curType.FullName,
                };
                using (var memoryStream = new MemoryStream())
                {
                    await ff.CopyToAsync(memoryStream);
                    fileModel.Bytes = memoryStream.ToArray();
                }
                fileList.Add(fileModel);
            }

            var filesPropertyInfo = FilesPropertyInfos.TryGetValue(curType, out PropertyInfo pi) ? pi : (pi = FilesPropertyInfos[curType] = curType.GetProperty("Files"));
            filesPropertyInfo.SetValue(typedModel, fileList);
            return await Create(typedModel);
        }

        [DisableFormValueModelBinding]
        [HttpPost(Consts.MultipartPrefix + "/{entityName}" + "/" + Consts.StreamSuffix)]
        public async Task<IActionResult> PostMultipartStream()
        {
            // Used to accumulate all the form url encoded key value pairs in the 
            // request.
            var formAccumulator = new KeyValueAccumulator();
            string targetFilePath = null;

            var contentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            var boundary = MultipartRequestHelper.GetBoundary(contentType, _config.MaxMultipartBoundaryLength);
            var reader = new MultipartReader(boundary.Value, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        targetFilePath = Path.GetTempFileName();
                        using (var targetStream = System.IO.File.Create(targetFilePath))
                        {
                            await section.Body.CopyToAsync(targetStream);
                        }
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

            // // Bind form data to a model
            // var user = new User();
            // var formValueProvider = new FormValueProvider(
            //     BindingSource.Form,
            //     new FormCollection(formAccumulator.GetResults()),
            //     CultureInfo.CurrentCulture);

            // var bindingSuccessful = await TryUpdateModelAsync(user, prefix: "",
            //     valueProvider: formValueProvider);
            // if (!bindingSuccessful)
            // {
            //     if (!ModelState.IsValid)
            //     {
            //         return BadRequest(ModelState);
            //     }
            // }

            // var uploadedData = new UploadedData()
            // {
            //     Name = user.Name,
            //     Age = user.Age,
            //     Zipcode = user.Zipcode,
            //     FilePath = targetFilePath
            // };
            // return Json(uploadedData);
            throw new NotImplementedException();
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
        public async Task<IActionResult> Put(string id, [FromBody] JObject model)
        {
            if (!ModelState.IsValid || model == null)
                return new BadRequestObjectResult(new
                {
                    message = "Bad or missing data",
                    data = model
                });
            var typedModel = model.ToObject(_workContext.CurrentType);
            var umi = UpdateMethodInfo ?? (UpdateMethodInfo = _crudService.GetType().GetMethod("Update"));
            var res = await umi.Invoke(_crudService, new[] { id, typedModel });
            return (res as ServiceResponse).ToActionResult();
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
            var cmi = CreateMethodInfo ?? (CreateMethodInfo = _crudService.GetType().GetMethod("Create"));
            var res = await cmi.Invoke(_crudService, new[] { model });
            return (res as ServiceResponse).ToActionResult();
        }
        #endregion
    }
}