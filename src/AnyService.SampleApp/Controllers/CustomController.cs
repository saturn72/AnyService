using AnyService.SampleApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnyService.SampleApp.Controllers
{
    [Route("api/my-great-route")]
    [ApiController]
    public class CustomController : ControllerBase
    {

        [HttpPost]
        public string Post([FromBody] CustomModel model)
        {
            return model.Value?.ToLower() == "ping" ? "pong" : "not-pong";
        }
    }
}
