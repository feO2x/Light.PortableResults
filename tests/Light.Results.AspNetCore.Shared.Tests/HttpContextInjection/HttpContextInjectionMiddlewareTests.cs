using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.AspNetCore.Shared.HttpContextInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.Results.AspNetCore.Shared.Tests.HttpContextInjection;

public sealed class HttpContextInjectionMiddlewareTests
{
    private readonly NextMiddlewareSpy _nextMiddleware = new ();

    [Fact]
    public async Task InvokeAsync_InjectsHttpContextToAllServicesImplementingIInjectHttpContext()
    {
        await using var serviceProvider = new ServiceCollection()
           .AddScoped<IInjectHttpContext, InjectHttpContextSpyA>()
           .AddScoped<IInjectHttpContext, InjectHttpContextSpyB>()
           .BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();
        var middleware = new HttpContextInjectionMiddleware(_nextMiddleware.InvokeAsync);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        await middleware.InvokeAsync(httpContext);

        var spies = scope.ServiceProvider.GetServices<IInjectHttpContext>();
        foreach (var spy in spies)
        {
            spy.Should().BeAssignableTo<BaseInjectHttpContextSpy>().Which.HttpContextMustHaveBeenSet();
        }

        _nextMiddleware.InvokeAsyncMustHaveBeenCalledWith(httpContext);
    }

    [Fact]
    public async Task InvokeAsync_FallsBackToIEnumerableOfT_WhenCustomServiceProviderDoesNotReturnArrayOnGetServices()
    {
        var spies = new List<BaseInjectHttpContextSpy>
        {
            new InjectHttpContextSpyA(),
            new InjectHttpContextSpyB()
        };
        var serviceProvider = new ServiceProviderStub(spies);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        var middleware = new HttpContextInjectionMiddleware(_nextMiddleware.InvokeAsync);

        await middleware.InvokeAsync(httpContext);

        foreach (var spy in spies)
        {
            spy.HttpContextMustHaveBeenSet();
        }

        _nextMiddleware.InvokeAsyncMustHaveBeenCalledWith(httpContext);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenNextMiddlewareIsNull()
    {
        var act = () => new HttpContextInjectionMiddleware(null!);

        act.Should().Throw<ArgumentNullException>().Where(x => x.ParamName == "next");
    }

    private abstract class BaseInjectHttpContextSpy : IInjectHttpContext
    {
        private HttpContext? _httpContext;

        public HttpContext HttpContext
        {
            set => _httpContext = value ?? throw new ArgumentNullException(nameof(HttpContext));
        }

        public void HttpContextMustHaveBeenSet() => _httpContext.Should().NotBeNull();
    }

    private sealed class InjectHttpContextSpyA : BaseInjectHttpContextSpy;

    private sealed class InjectHttpContextSpyB : BaseInjectHttpContextSpy;

    private sealed class NextMiddlewareSpy
    {
        private HttpContext? _capturedContext;

        public Task InvokeAsync(HttpContext context)
        {
            _capturedContext = context;
            return Task.CompletedTask;
        }

        public void InvokeAsyncMustHaveBeenCalledWith(HttpContext httpContext) =>
            _capturedContext.Should().BeSameAs(httpContext);
    }

    private sealed class ServiceProviderStub : IServiceProvider
    {
        private readonly List<BaseInjectHttpContextSpy> _spies;

        public ServiceProviderStub(List<BaseInjectHttpContextSpy> spies) => _spies = spies;

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IEnumerable<IInjectHttpContext>))
            {
                return _spies;
            }

            throw new InvalidOperationException(
                "This stub is only configured to return services of type IEnumerable<IInjectHttpContext>"
            );
        }
    }
}
