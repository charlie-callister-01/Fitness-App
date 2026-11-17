using Core.Storage;

namespace Core.Tests.Storage;

public class InMemoryDataStoreTests : DataStoreContract
{
    protected override IDataStore CreateEmptyStore() => new InMemoryDataStore();
}
