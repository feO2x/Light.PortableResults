using Microsoft.AspNetCore.Builder;

namespace Light.Results.AspNetCore.Shared.HttpContextInjection;

public static class HttpContextInjectionMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpContextInjection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpContextInjectionMiddleware>();
    }
}
