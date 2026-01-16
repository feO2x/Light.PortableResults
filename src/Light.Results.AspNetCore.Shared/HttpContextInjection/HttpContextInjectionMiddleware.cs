using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Results.AspNetCore.Shared.HttpContextInjection;

public sealed class HttpContextInjectionMiddleware
{
    private readonly RequestDelegate _next;

    public HttpContextInjectionMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var scopedServicesWithNeedForHttpContext = context.RequestServices.GetServices<IInjectHttpContext>();
        InjectHttpContext(context, scopedServicesWithNeedForHttpContext);
        await _next(context);
    }

    private static void InjectHttpContext(
        HttpContext context,
        IEnumerable<IInjectHttpContext> scopedServicesWithNeedForHttpContext
    )
    {
        switch (scopedServicesWithNeedForHttpContext)
        {
            case IInjectHttpContext[] array:
                foreach (var scopedService in array)
                {
                    scopedService.HttpContext = context;
                }

                break;
            default:
                foreach (var scopedService in scopedServicesWithNeedForHttpContext)
                {
                    scopedService.HttpContext = context;
                }

                break;
        }
    }
}
