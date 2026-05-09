using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EnglishLearner.Infrastructure.Security;

public sealed class DpapiKeyStore
{
    private readonly string _filePath;

    public DpapiKeyStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EnglishLearner", "apikey.dat");
    }

    public void SaveKey(string apiKey)
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);

        var plain = Encoding.UTF8.GetBytes(apiKey);
        var cipher = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, cipher);
    }

    public string? LoadKey()
    {
        if (!File.Exists(_filePath)) return null;

        var cipher = File.ReadAllBytes(_filePath);
        var plain = ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plain);
    }

    public void DeleteKey()
    {
        if (File.Exists(_filePath)) File.Delete(_filePath);
    }
}
