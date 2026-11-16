using Core.Ids;

namespace Core.Tests.Ids;

/// <summary>Deterministic ID generator for tests: prefix-1, prefix-2, ...</summary>
internal sealed class SequentialIdGenerator(string prefix) : IIdGenerator
{
    private int _next;

    public string NewId() => $"{prefix}-{++_next}";
}
