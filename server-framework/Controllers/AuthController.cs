using System;
using System.Web;
using System.Web.Http;

namespace OnlyOfficeServerFramework.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        [HttpGet]
        [Route("status")]
        public IHttpActionResult GetStatus()
        {
            // For now, hardcoded response (no database, no sessions yet)
            // This will be replaced with actual session checking later
            var response = new
            {
                isAuthenticated = false,
                userId = (Guid?)null,
                username = (string)null
            };

            return Ok(response);
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            // TODO: Implement actual login logic with database and sessions
            // For now, just return error
            return BadRequest("Login not implemented yet");
        }

        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout()
        {
            // TODO: Implement actual logout logic
            // For now, just return success
            return Ok(new { message = "Logout not implemented yet" });
        }
    }

    // Request model for login
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
