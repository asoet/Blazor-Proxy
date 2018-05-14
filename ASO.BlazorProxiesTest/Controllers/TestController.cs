using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASO.BlazorProxiesTest.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ASO.BlazorProxiesTest.Controllers
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public class TestController : Controller
    {
        // GET: api/<controller>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public TestModel Get(int id)
        {
            return new TestModel() { Id = Guid.NewGuid(), Name = "test" };
        }

        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]TestModel value)
        {
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]TestModel value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
