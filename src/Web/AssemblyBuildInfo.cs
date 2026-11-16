using System.Reflection;
using Ui;

namespace Web;

internal sealed class AssemblyBuildInfo : IBuildInfo
{
    public string Identifier { get; } = BuildIdentifier.Shorten(
        typeof(AssemblyBuildInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
}
