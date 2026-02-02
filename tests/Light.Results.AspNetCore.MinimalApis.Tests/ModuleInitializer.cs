using System.Runtime.CompilerServices;
using VerifyTests;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() => VerifyHttp.Initialize();
}
