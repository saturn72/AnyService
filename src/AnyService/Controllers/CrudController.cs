using System.Reflection;
using System.Threading.Tasks;
using AnyService.Services;
using Microsoft.AspNetCore.Mvc;
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
            var cmi = CreateMethodInfo ?? (CreateMethodInfo = _crudService.GetType().GetMethod("Create"));
            var res = await cmi.Invoke(_crudService, new[] { typedModel });
            return (res as ServiceResponse).ToActionResult();
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
    }
}