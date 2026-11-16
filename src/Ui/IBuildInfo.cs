namespace Ui;

/// <summary>Identifies the running build, supplied by the host.</summary>
public interface IBuildInfo
{
    string Identifier { get; }
}
