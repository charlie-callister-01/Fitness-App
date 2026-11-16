namespace Core.Ids;

/// <summary>Source of new record IDs. Rules take this so tests can use deterministic IDs.</summary>
public interface IIdGenerator
{
    string NewId();
}
