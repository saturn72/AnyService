using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AnyService.Controllers
{
    public abstract class AnyServiceControllerBase<T> : AnyServiceControllerBase
    {

    }
    public abstract class AnyServiceControllerBase : ControllerBase
    {
        protected readonly static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        protected virtual JsonResult JsonResult(object value) => new JsonResult(value, JsonSerializerOptions);
    }
}