namespace OnlyOfficeServer.Services;

public class WebDavMiddleware
{
    private readonly RequestDelegate _next;

    public WebDavMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Handle WebDAV methods that aren't standard HTTP methods
        if (context.Request.Method == "PROPFIND" || 
            context.Request.Method == "PROPPATCH" ||
            context.Request.Method == "MKCOL" ||
            context.Request.Method == "COPY" ||
            context.Request.Method == "MOVE" ||
            context.Request.Method == "LOCK" ||
            context.Request.Method == "UNLOCK")
        {
            // Add method override header for routing
            context.Request.Headers["X-HTTP-Method-Override"] = context.Request.Method;
            
            // Change method to POST for routing
            context.Request.Method = "POST";
        }

        await _next(context);
    }
}