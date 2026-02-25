# AGENTS.md for Production Code

## Overview of the projects

- Light.PortableResults contains the core implementation which is framework-agnostic. As the only project, it uses .NET
  Standard 2.0 instead of .NET 10 so that it can be used in all different contexts. It supports Native AOT, although
  this is not explicit, as `<IsAotCompatible>` cannot be set on .NET Standard 2.0 projects.
- Light.PortableResults.AspNetCore.Shared is a project containing shared code for the ASP.NET Core integration for
  Minimal APIs as well as the upcoming MVC project. `<IsAotCompatible>` is set to true.
- Light.PortableResults.AspNetCore.MinimalApis contains specific types for Minimal API integration. `<IsAotCompatible>`
  is set to true.
- Light.PortableResults.AspNetCore.Mvc contains specific types for MVC integration. `<IsAotCompatible>` is not set
  because MVC
  itself is not AOT-compatible.
