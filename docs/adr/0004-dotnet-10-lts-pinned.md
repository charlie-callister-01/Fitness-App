# .NET 10 (LTS) is the pinned platform version

Verified on 2026-11-16 against Microsoft's .NET support policy: .NET 10 is an LTS release, first released 11 November 2025, supported until 14 November 2028 (three years from release). .NET 11 is a short-term (STS) release and is not used.

Decision:
- Target `net10.0` in every project (set once in the shared build props).
- Pin the SDK in `global.json` (10.0.401, rolling forward only to newer feature bands) so every machine and CI run builds with the same SDK.
- Use the standalone Blazor WebAssembly PWA template (no server project) for `Web`, a Razor class library for `Ui`, and plain class libraries for `Core` and `Core.Tests`.
- Package versions are declared once through central package management.

Revisit when .NET 10 approaches end of support (plan the move to the next LTS well before November 2028, which is after the HYROX target date).

Source: Microsoft .NET and .NET Core Support Policy (dotnet.microsoft.com/platform/support/policy/dotnet-core).
