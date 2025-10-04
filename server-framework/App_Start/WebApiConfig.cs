using System.Web.Http;
using System.Web.Http.Cors;

namespace OnlyOfficeServerFramework
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Enable CORS for Angular frontend
            var cors = new EnableCorsAttribute("http://localhost:4200", "*", "*");
            cors.SupportsCredentials = true;
            config.EnableCors(cors);

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}