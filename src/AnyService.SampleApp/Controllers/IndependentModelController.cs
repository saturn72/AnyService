using System;
using System.Collections.Generic;
using AnyService.SampleApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnyService.SampleApp.Controllers
{
    [Route("api/Independent")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "IndependentModel1", "IndependentModel2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "IndependentModel_" + id;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] IndependentModel model)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public int Put(int id, [FromBody] IndependentModel model)
        {
            return id;
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public int Delete(int id)
        {
            return new Random().Next();
        }
    }
}
