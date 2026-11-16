namespace Core.Ids;

/// <summary>Generates GUIDs in canonical lowercase form (for example 3f2504e0-4f89-11d3-9a0c-0305e82c3301).</summary>
public sealed class GuidIdGenerator : IIdGenerator
{
    public string NewId() => Guid.NewGuid().ToString("D");
}
