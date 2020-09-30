using AnyService.SampleApp.Models;
using AnyService.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace AnyService.SampleApp.Controllers
{
    [Route("api/standalone")]
    [ApiController]
    public class UsesCrudServiceController : ControllerBase
    {
        private readonly ICrudService<DependentModel> _srv;

        public UsesCrudServiceController(ICrudService<DependentModel> srv)
        {
            _srv = srv;
        }
        [HttpGet]
        public string Get()
        {
            return DateTime.UtcNow.ToIso8601();
        }
    }

}