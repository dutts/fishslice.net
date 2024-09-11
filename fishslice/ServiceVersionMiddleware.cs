using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace fishslice;

internal class ServiceVersionMiddleware(RequestDelegate next)
{
    private static readonly string Service;

    static ServiceVersionMiddleware()
    {
        Service = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Service"] = Service;
            return Task.CompletedTask;
        });

        await next(context);
    }
}