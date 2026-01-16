using System;
using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.AspNetCore.Shared.HttpContextInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.Results.AspNetCore.Shared.Tests.HttpContextInjection;

public sealed class HttpContextInjectionMiddlewareTests : IAsyncDisposable
{
    private readonly NextMiddlewareSpy _nextMiddleware = new ();
    private readonly AsyncServiceScope _scope;
    private readonly ServiceProvider _serviceProvider;

    public HttpContextInjectionMiddlewareTests()
    {
        _serviceProvider = new ServiceCollection()
           .AddScoped<IInjectHttpContext, InjectHttpContextSpyA>()
           .AddScoped<IInjectHttpContext, InjectHttpContextSpyB>()
           .BuildServiceProvider();
        _scope = _serviceProvider.CreateAsyncScope();
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task InvokeAsync_InjectsHttpContextToAllServicesImplementingIInjectHttpContext()
    {
        var middleware = new HttpContextInjectionMiddleware(_nextMiddleware.InvokeAsync);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _scope.ServiceProvider
        };

        await middleware.InvokeAsync(httpContext);

        var spies = _scope.ServiceProvider.GetServices<IInjectHttpContext>();
        foreach (var spy in spies)
        {
            spy.Should().BeAssignableTo<BaseInjectHttpContextSpy>().Which.HttpContextMustHaveBeenSet();
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
}
