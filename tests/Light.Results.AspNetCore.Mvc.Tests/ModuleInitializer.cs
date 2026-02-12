using System;
using System.Runtime.CompilerServices;
using VerifyTests;

namespace Light.Results.AspNetCore.Mvc.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // This is required for serialization tests that do not rely on JsonSerializerContext.
        AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);
        VerifyHttp.Initialize();
    }
}
