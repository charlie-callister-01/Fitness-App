using Core.Storage;
using Web.Storage;

namespace Web.Tests.Storage;

public class BrowserStorageEnvironmentTests
{
    private sealed class FakeEnvironmentInterop : IStorageEnvironmentInterop
    {
        public string Persistence { get; set; } = "granted";
        public StorageUsage? Estimate { get; set; }
        public string Lock { get; set; } = "obtained";
        public bool Marker { get; set; }
        public Exception? Throws { get; set; }

        private T Answer<T>(T value) => Throws is null ? value : throw Throws;

        public Task<string> RequestPersistenceAsync() => Task.FromResult(Answer(Persistence));
        public Task<StorageUsage?> EstimateAsync() => Task.FromResult(Answer(Estimate));
        public Task<string> TryTakeWriterLockAsync() => Task.FromResult(Answer(Lock));
        public Task<bool> ReadClearedDataMarkerAsync() => Task.FromResult(Answer(Marker));

        public Task WriteClearedDataMarkerAsync()
        {
            Answer(true);
            Marker = true;
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData("granted", PersistenceStatus.Granted)]
    [InlineData("notGranted", PersistenceStatus.NotGranted)]
    [InlineData("unsupported", PersistenceStatus.Unsupported)]
    [InlineData("something else", PersistenceStatus.Unsupported)]
    public async Task Persistence_answer_maps_to_status(string answer, PersistenceStatus expected)
    {
        var environment = new BrowserStorageEnvironment(new FakeEnvironmentInterop { Persistence = answer });

        Assert.Equal(expected, await environment.RequestPersistenceAsync());
    }

    [Fact]
    public async Task Estimate_maps_the_module_answer()
    {
        var environment = new BrowserStorageEnvironment(new FakeEnvironmentInterop { Estimate = new StorageUsage(1234, 5_000_000_000) });

        Assert.Equal(new StorageEstimate(1234, 5_000_000_000), await environment.EstimateAsync());
    }

    [Fact]
    public async Task Estimate_is_unknown_when_the_module_cannot_say()
    {
        var environment = new BrowserStorageEnvironment(new FakeEnvironmentInterop { Estimate = null });

        Assert.Null(await environment.EstimateAsync());
    }

    [Theory]
    [InlineData("obtained", WriterLockResult.Obtained)]
    [InlineData("heldElsewhere", WriterLockResult.HeldElsewhere)]
    [InlineData("unsupported", WriterLockResult.Unsupported)]
    [InlineData("unknown", WriterLockResult.Unknown)]
    [InlineData("something else", WriterLockResult.Unknown)]
    public async Task Writer_lock_answer_maps_to_result(string answer, WriterLockResult expected)
    {
        var environment = new BrowserStorageEnvironment(new FakeEnvironmentInterop { Lock = answer });

        Assert.Equal(expected, await environment.TryTakeWriterLockAsync());
    }

    [Fact]
    public async Task The_marker_passes_through()
    {
        var environment = new BrowserStorageEnvironment(new FakeEnvironmentInterop());

        Assert.False(await environment.ReadClearedDataMarkerAsync());
        await environment.WriteClearedDataMarkerAsync();
        Assert.True(await environment.ReadClearedDataMarkerAsync());
    }

    [Fact]
    public async Task A_failing_browser_gives_the_safe_answers_and_never_throws()
    {
        var environment = new BrowserStorageEnvironment(new FakeEnvironmentInterop { Throws = new InvalidOperationException("no module") });

        Assert.Equal(PersistenceStatus.Unsupported, await environment.RequestPersistenceAsync());
        Assert.Null(await environment.EstimateAsync());
        Assert.Equal(WriterLockResult.Unknown, await environment.TryTakeWriterLockAsync());
        Assert.False(await environment.ReadClearedDataMarkerAsync());
        await environment.WriteClearedDataMarkerAsync();
    }
}
