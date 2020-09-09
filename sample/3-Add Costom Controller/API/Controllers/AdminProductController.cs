using API.Domain;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("product")]
    [ApiController]
    public class AdminProductController : ControllerBase
    {
        [HttpPost]
        public string Post([FromBody] Product model)
        {
            return "hellow from custom controller";
        }
    }
}
