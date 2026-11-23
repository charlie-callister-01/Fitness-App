using System.Diagnostics;

namespace Web.BrowserTests.App;

/// <summary>
/// The Release publish of <c>src/Web</c> that the whole-app tests serve. CI publishes first and names the folder in
/// <see cref="DirectoryVariable"/>; otherwise the run publishes once into the test output.
/// </summary>
public static class PublishedApp
{
    /// <summary>A publish output folder (the one holding <c>wwwroot</c>) made before the run, absolute or from the repository root. Unset: the run publishes itself.</summary>
    public const string DirectoryVariable = "BROWSER_TESTS_APP_DIR";

    /// <summary>Returns the published <c>wwwroot</c> folder, publishing the Web project (Release) first when no folder was given.</summary>
    public static async Task<string> WwwrootAsync()
    {
        var given = Environment.GetEnvironmentVariable(DirectoryVariable);
        // A relative folder is from the repository root, where the commands in the README run.
        var publishDirectory = string.IsNullOrWhiteSpace(given) ? await PublishAsync() : Path.GetFullPath(given, RepositoryRoot());

        var wwwroot = Path.Combine(publishDirectory, "wwwroot");
        if (!File.Exists(Path.Combine(wwwroot, "index.html")))
        {
            throw new InvalidOperationException($"No published app at '{wwwroot}'. Run `dotnet publish src/Web -c Release -o <dir>` and set {DirectoryVariable} to <dir>, or unset it.");
        }

        return wwwroot;
    }

    private static async Task<string> PublishAsync()
    {
        var project = Path.Combine(RepositoryRoot(), "src", "Web", "Web.csproj");
        var output = Path.Combine(AppContext.BaseDirectory, "published-app");

        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] { "publish", project, "-c", "Release", "-o", output, "-nodeReuse:false", "-v:quiet" })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start `dotnet publish`.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"`dotnet publish` of the Web project failed:\n{await stdout}\n{await stderr}");
        }

        return output;
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FitnessApp.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not find FitnessApp.sln above the test output.");
    }
}
