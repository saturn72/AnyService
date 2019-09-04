using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyService.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnyService.Controllers
{
    [Route(Consts.AnyServiceControllerName + "/{entityName}")]
    [ApiController]
    public class CrudController : ControllerBase
    {
        #region fields
        private readonly dynamic _crudService;
        private readonly AnyServiceWorkContext _workContext;
        private static MethodInfo CreateMethodInfo;
        private static MethodInfo UpdateMethodInfo;
        private static IDictionary<Type, PropertyInfo> FilesPropertyInfos = new Dictionary<Type, PropertyInfo>();
        #endregion
        #region ctor
        public CrudController(dynamic crudService, AnyServiceWorkContext workContext)
        {
            _crudService = crudService;
            _workContext = workContext;
        }
        #endregion

        [HttpPost("")]
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
        [HttpPost("form")]
        public async Task<IActionResult> PostForm()
        {
            if (!Request.HasFormContentType) return BadRequest();
            var curType = _workContext.CurrentType;
            var form = Request.Form;
            var typedModel = JsonConvert.DeserializeObject(form["model"], curType);

            var fileList = new List<FileModel>();
            foreach (var ff in form.Files)
            {
                var fileModel = new FileModel
                {
                    FileName = ff.FileName,
                    Stream = new MemoryStream(),
                    ContainerKey = curType.FullName,
                };
                await ff.CopyToAsync(fileModel.Stream);
                fileList.Add(fileModel);
            }

            var filesPropertyInfo = FilesPropertyInfos.TryGetValue(curType, out PropertyInfo pi) ? pi : (pi = FilesPropertyInfos[curType] = curType.GetProperty("Files"));

            filesPropertyInfo.SetValue(typedModel, fileList);

            return await Create(typedModel);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var res = await _crudService.GetById(id);
            return (res as ServiceResponse).ToActionResult();
        }
        [HttpGet("")]
        public async Task<IActionResult> GetAll()
        {
            var res = await _crudService.GetAll();
            return (res as ServiceResponse).ToActionResult();
        }
        [HttpPut("{id}")]
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
        [HttpDelete("{id}")]
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