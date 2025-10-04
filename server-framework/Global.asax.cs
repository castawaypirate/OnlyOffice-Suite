using System;
using System.Web;
using System.Web.Http;
using OnlyOfficeServerFramework.Data;
using OnlyOfficeServerFramework.Services;

namespace OnlyOfficeServerFramework
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // Initialize database and seed data
            using (var context = new AppDbContext())
            {
                DatabaseSeederService.SeedDatabase(context);
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
        }
    }
}