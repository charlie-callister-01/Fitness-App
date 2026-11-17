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

/// <summary>The outcome of a call that returns nothing: it succeeded (no failure) or failed. Never thrown for an expected storage failure.</summary>
public record IndexedDbResult(IndexedDbFailure? Failure);

/// <summary>The outcome of a call that returns a value: <see cref="Value"/> is set exactly when <see cref="IndexedDbResult.Failure"/> is null.</summary>
public sealed record IndexedDbResult<T>(T? Value, IndexedDbFailure? Failure) : IndexedDbResult(Failure);

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

    public async Task<IndexedDbResult> OpenAsync(string databaseName) => new((await InvokeAsync("open", databaseName)).Failure);

    public async Task<IndexedDbResult<LoadedData>> LoadAllAsync()
    {
        var call = await InvokeAsync("loadAll");
        if (call.Failure is not null)
        {
            return new(default, call.Failure);
        }

        try
        {
            return new(JsonSerializer.Deserialize<LoadedData>(call.Json!, Wire), null);
        }
        catch (JsonException e)
        {
            return new(default, Unexpected(e));
        }
    }

    public async Task<IndexedDbResult> ApplyAsync(IReadOnlyList<IndexedDbOperation> ops, IReadOnlyDictionary<string, string?>? meta = null) =>
        new((await InvokeAsync("apply", JsonSerializer.Serialize(new { ops, meta }, Wire))).Failure);

    public async Task<IndexedDbResult> ReplaceAllAsync(
        IReadOnlyDictionary<string, IReadOnlyList<RawRecord>> records, IReadOnlyDictionary<string, string?>? meta = null) =>
        new((await InvokeAsync("replaceAll", JsonSerializer.Serialize(new { records, meta }, Wire))).Failure);

    public async Task<IndexedDbResult> CloseAsync() => new((await InvokeAsync("close")).Failure);

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }

    private readonly record struct Call(string? Json, IndexedDbFailure? Failure);

    private sealed record Envelope(bool Ok, IndexedDbFailureKind? Kind, string? Detail);

    private static IndexedDbFailure Unexpected(Exception exception) => new(IndexedDbFailureKind.Unexpected, exception.Message);

    /// <summary>Calls a module function and reads its envelope: the raw JSON on success, or the failure it reported (or that calling it caused).</summary>
    private async Task<Call> InvokeAsync(string function, params string[] args)
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            var json = await _module.InvokeAsync<string>(function, args);
            var envelope = JsonSerializer.Deserialize<Envelope>(json, Wire)!;
            return envelope.Ok
                ? new(json, null)
                : new(null, new IndexedDbFailure(envelope.Kind ?? IndexedDbFailureKind.Unexpected, envelope.Detail ?? ""));
        }
        catch (Exception exception) when (exception is JSException or JsonException or InvalidOperationException)
        {
            return new(null, Unexpected(exception));
        }
    }
}
