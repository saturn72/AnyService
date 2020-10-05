using AnyService.SampleApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnyService.SampleApp.Controllers
{
    [Route("v1/my-great-route")]
    [ApiController]
    public class CustomController : ControllerBase
    {

        [HttpPost]
        public string Post([FromBody] CustomEntity model)
        {
            return model.Value?.ToLower() == "ping" ? "pong" : "not-pong";
        }
    }
}
