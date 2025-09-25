using System;
using System.Collections.Generic;
using System.Web.Http;

namespace OnlyOfficeServerFramework.Controllers
{
    public class TestController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            var response = new
            {
                message = "Hello from OnlyOffice Server Framework!",
                timestamp = DateTime.UtcNow,
                status = "OK"
            };

            return Ok(response);
        }

        [HttpGet]
        [Route("api/test/ping")]
        public IHttpActionResult Ping()
        {
            return Ok(new { message = "Pong!", timestamp = DateTime.UtcNow });
        }

        [HttpPost]
        public IHttpActionResult Post([FromBody] dynamic data)
        {
            var response = new
            {
                message = "Data received successfully",
                receivedData = data,
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
    }
}