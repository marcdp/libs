using System.Runtime.CompilerServices;
using DProjects.Fs.Http;
using DProjects.Utils;

namespace DProjects.Fs.Test;

public class FilesystemUrlSecurityTests {

    // consts
    private const string Username = "TEST_USER";
    private const string Password = "SUPER_SECRET_PASSWORD+";
    private const string Token = "SUPER_SECRET_TOKEN+";
    private const string ApiKey = "SUPER_SECRET_API_KEY+";

    // methods
    [Fact]
    public void HttpUrlRetainsUsernameAndSafeOptionsButOmitsSecrets() {
        var input = "https://" + Username + ":" + UrlUtils.UrlEncode(Password) + "@example.com/files?authScheme=basic&isReadonly=true&token=" +
            UrlUtils.UrlEncode(Token) + "&apiKey=" + UrlUtils.UrlEncode(ApiKey);
        using var filesystem = new FilesystemHttp(new Uri(input), 1024, FilesystemHttp.AuthSchemes.Basic, true);

        Assert.Contains(Username, filesystem.Url);
        Assert.Contains("authScheme=basic", filesystem.Url);
        Assert.Contains("isReadonly=true", filesystem.Url);
        AssertUrlDoesNotContainSecret(filesystem, Password);
        AssertUrlDoesNotContainSecret(filesystem, Token);
        AssertUrlDoesNotContainSecret(filesystem, ApiKey);
    }
    [Fact]
    public void HttpApiKeyUrlOmitsEntireSecretUserInfo() {
        var input = "https://" + UrlUtils.UrlEncode(ApiKey) + "@example.com/files?authScheme=apiKey";
        using var filesystem = new FilesystemHttp(new Uri(input), 1024, FilesystemHttp.AuthSchemes.ApiKey, false);

        Assert.DoesNotContain("@", filesystem.Url);
        AssertUrlDoesNotContainSecret(filesystem, ApiKey);
    }
    [Fact]
    public void SmbUrlRetainsUsernameButOmitsPassword() {
        var filesystem = (FilesystemSmb)RuntimeHelpers.GetUninitializedObject(typeof(FilesystemSmb));
        SetField(filesystem, "mUsername", Username);
        SetField(filesystem, "mPassword", Password);
        SetField(filesystem, "mHost", "server");
        SetField(filesystem, "mShare", "share");

        Assert.Contains(Username, filesystem.Url);
        AssertUrlDoesNotContainSecret(filesystem, Password);
    }

    // methods (private)
    private static void AssertUrlDoesNotContainSecret(IFilesystem filesystem, string secret) {
        Assert.DoesNotContain(secret, filesystem.Url);
        Assert.DoesNotContain(UrlUtils.UrlEncode(secret), filesystem.Url);
    }
    private static void SetField(FilesystemSmb filesystem, string name, string value) {
        var field = typeof(FilesystemSmb).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(filesystem, value);
    }
}
