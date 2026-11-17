using Core.Storage;
using Web.Storage;

namespace Web.Tests.Storage;

public class BrowserStorageEnvironmentTests
{
    private sealed class FakeEnvironmentInterop : IStorageEnvironmentInterop
    {
        public string Persistence { get; set; } = "granted";
        public string Estimate { get; set; } = "";
        public bool Lock { get; set; } = true;
        public bool Marker { get; set; }
        public Exception? Throws { get; set; }

        private T Answer<T>(T value) => Throws is null ? value : throw Throws;

        public Task<string> RequestPersistenceAsync() => Task.FromResult(Answer(Persistence));
        public Task<string> EstimateAsync() => Task.FromResult(Answer(Estimate));
        public Task<bool> TryTakeWriterLockAsync() => Task.FromResult(Answer(Lock));
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
    public async Task Estimate_is_parsed_from_the_module_json()
    {
        var environment = new BrowserStorageEnvironment(new FakeEnvironmentInterop { Estimate = """{"usage":1234,"quota":5000000000}""" });

        Assert.Equal(new StorageEstimate(1234, 5_000_000_000), await environment.EstimateAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"usage":1}""")]
    [InlineData("""{"usage":"a","quota":2}""")]
    public async Task Estimate_is_unknown_when_the_module_cannot_say_or_says_nonsense(string answer)
    {
        var environment = new BrowserStorageEnvironment(new FakeEnvironmentInterop { Estimate = answer });

        Assert.Null(await environment.EstimateAsync());
    }

    [Fact]
    public async Task Writer_lock_and_marker_pass_through()
    {
        var fake = new FakeEnvironmentInterop { Lock = false };
        var environment = new BrowserStorageEnvironment(fake);

        Assert.False(await environment.TryTakeWriterLockAsync());
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
        Assert.False(await environment.TryTakeWriterLockAsync());
        Assert.False(await environment.ReadClearedDataMarkerAsync());
        await environment.WriteClearedDataMarkerAsync();
    }
}
