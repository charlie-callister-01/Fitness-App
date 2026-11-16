using System.Reflection;
using Ui;

namespace Web;

internal sealed class AssemblyBuildInfo : IBuildInfo
{
    public string Identifier { get; } = Resolve();

    private static string Resolve()
    {
        var version = typeof(AssemblyBuildInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return version ?? "dev";
    }
}
