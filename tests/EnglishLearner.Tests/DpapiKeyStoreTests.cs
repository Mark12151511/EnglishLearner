using EnglishLearner.Infrastructure.Security;
using Xunit;

namespace EnglishLearner.Tests;

public class DpapiKeyStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly DpapiKeyStore _store;

    public DpapiKeyStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"test_apikey_{Guid.NewGuid()}.dat");
        _store = new DpapiKeyStore(_tempFile);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        const string key = "sk-ant-test-key-12345";
        _store.SaveKey(key);
        var loaded = _store.LoadKey();

        Assert.Equal(key, loaded);
    }

    [Fact]
    public void Load_NoFile_ReturnsNull()
    {
        var result = _store.LoadKey();
        Assert.Null(result);
    }

    [Fact]
    public void Delete_RemovesFile()
    {
        _store.SaveKey("test");
        Assert.True(File.Exists(_tempFile));

        _store.DeleteKey();
        Assert.False(File.Exists(_tempFile));
    }

    [Fact]
    public void Delete_NoFile_DoesNotThrow()
    {
        var ex = Record.Exception(() => _store.DeleteKey());
        Assert.Null(ex);
    }

    [Fact]
    public void Save_OverwritesPrevious()
    {
        _store.SaveKey("old-key");
        _store.SaveKey("new-key");

        Assert.Equal("new-key", _store.LoadKey());
    }

    [Fact]
    public void SavedFile_IsNotPlainText()
    {
        _store.SaveKey("sk-secret-key");
        var bytes = File.ReadAllBytes(_tempFile);
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.NotEqual("sk-secret-key", text);
    }

    [Fact]
    public void Save_CreatesDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
        var file = Path.Combine(dir, "apikey.dat");
        try
        {
            var store = new DpapiKeyStore(file);
            store.SaveKey("test-key");

            Assert.True(File.Exists(file));
            Assert.Equal("test-key", store.LoadKey());
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
