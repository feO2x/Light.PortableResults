using Microsoft.AspNetCore.Http;

namespace Light.Results.AspNetCore.Shared.HttpContextInjection;

public interface IInjectHttpContext
{
    HttpContext HttpContext { set; }
}
