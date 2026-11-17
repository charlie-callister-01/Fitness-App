using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Web.Storage;

/// <summary>Failure kinds reported by the JavaScript module (wire names are lower case).</summary>
public enum IndexedDbFailureKind
{
    /// <summary>A schema upgrade is blocked by another open window.</summary>
    Blocked,
    /// <summary>The browser refused the write because storage is full.</summary>
    Quota,
    /// <summary>IndexedDB is missing or refused (private browsing, security error).</summary>
    Unavailable,
    /// <summary>The module's connection was closed (another tab upgraded, or the browser closed it); opening again fixes it.</summary>
    NotOpen,
    /// <summary>Anything else; <see cref="IndexedDbFailure.Detail"/> carries the diagnostics.</summary>
    Unexpected,
}

public sealed record IndexedDbFailure(IndexedDbFailureKind Kind, string Detail);

public sealed record RawRecord(string Key, string Json);

public sealed record LoadedData(
    IReadOnlyDictionary<string, IReadOnlyList<RawRecord>> Records,
    IReadOnlyDictionary<string, string> Meta);

/// <summary>One put or delete in a batch. <see cref="Json"/> is null for a delete.</summary>
public sealed record IndexedDbOperation(string Op, string Kind, string Key, string? Json)
{
    public static IndexedDbOperation Put(string kind, string key, string json) => new("put", kind, key, json);
    public static IndexedDbOperation Delete(string kind, string key) => new("delete", kind, key, null);
}

/// <summary>Success value or a failure; never throws for an expected storage failure.</summary>
public sealed record IndexedDbResult<T>(T? Value, IndexedDbFailure? Failure)
{
    public bool Succeeded => Failure is null;
}

/// <summary>
/// Typed wrapper over <c>wwwroot/js/indexeddb-store.js</c>. Strings go in and out of JavaScript; this class
/// only (de)serialises the small JSON envelopes. No storage rules live here.
/// </summary>
public sealed class IndexedDbInterop(IJSRuntime js) : IIndexedDbInterop, IAsyncDisposable
{
    public const string ModulePath = "./js/indexeddb-store.js";

    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private IJSObjectReference? _module;

    public Task<IndexedDbResult<object?>> OpenAsync(string databaseName) => CallAsync<object?>("open", databaseName);

    public Task<IndexedDbResult<LoadedData>> LoadAllAsync() => CallAsync<LoadedData>("loadAll");

    public Task<IndexedDbResult<object?>> ApplyAsync(IReadOnlyList<IndexedDbOperation> ops, IReadOnlyDictionary<string, string?>? meta = null) =>
        CallAsync<object?>("apply", JsonSerializer.Serialize(new { ops, meta }, Wire));

    public Task<IndexedDbResult<object?>> ReplaceAllAsync(
        IReadOnlyDictionary<string, IReadOnlyList<RawRecord>> records, IReadOnlyDictionary<string, string?>? meta = null) =>
        CallAsync<object?>("replaceAll", JsonSerializer.Serialize(new { records, meta }, Wire));

    public async Task<IndexedDbResult<string?>> GetMetaAsync(string key)
    {
        var result = await CallAsync<MetaValue>("getMeta", key);
        return new(result.Value?.Value, result.Failure);
    }

    public Task<IndexedDbResult<object?>> SetMetaAsync(string key, string value) => CallAsync<object?>("setMeta", key, value);

    public Task<IndexedDbResult<object?>> CloseAsync() => CallAsync<object?>("close");

    /// <summary>Test cleanup: deletes the whole database.</summary>
    public Task<IndexedDbResult<object?>> DestroyAsync(string databaseName) => CallAsync<object?>("destroy", databaseName);

    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }

    private sealed record MetaValue(string? Value);

    private sealed record Envelope(bool Ok, IndexedDbFailureKind? Kind, string? Detail);

    private async Task<IndexedDbResult<T>> CallAsync<T>(string function, params object[] args)
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            var json = await _module.InvokeAsync<string>(function, args);
            var envelope = JsonSerializer.Deserialize<Envelope>(json, Wire)!;
            if (!envelope.Ok)
                return new(default, new IndexedDbFailure(envelope.Kind ?? IndexedDbFailureKind.Unexpected, envelope.Detail ?? ""));
            return new(typeof(T) == typeof(object) ? default : JsonSerializer.Deserialize<T>(json, Wire), null);
        }
        catch (Exception e) when (e is JSException or JsonException or InvalidOperationException)
        {
            return new(default, new IndexedDbFailure(IndexedDbFailureKind.Unexpected, e.Message));
        }
    }
}
