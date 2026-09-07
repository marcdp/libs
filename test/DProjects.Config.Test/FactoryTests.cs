using DProjects.Config;
using DProjects.Config.Attributes;


namespace DProjects.Config.Test;

public class FactoryTests {
    [Fact]
    public void CreateFromUrl_PopulatesUriPartsAndConvertsQueryParameters() {
        var config = ConfigFactory.CreateFromUrl<UrlConfig>(
            "postgres://marc:secret@db.example.com:5433/main?poolSize=12&ssl=true");

        Assert.Equal("postgres", config.Scheme);
        Assert.Equal("/main", config.Path);
        Assert.Equal("db.example.com", config.Host);
        Assert.Equal(5433, config.Port);
        Assert.Equal("marc", config.User);
        Assert.Equal("secret", config.Password);
        Assert.Equal(12, config.PoolSize);
        Assert.True(config.Ssl);
    }

    [Fact]
    public void CreateFromUrl_WhenQueryParameterIsMissing_UsesConstructorDefault() {
        var config = ConfigFactory.CreateFromUrl<UrlConfig>(
            "postgres://marc:secret@db.example.com/main");

        Assert.Equal(5, config.PoolSize);
        Assert.False(config.Ssl);
    }

    [Fact]
    public void CreateFromUrl_WhenTypeHasNoPublicConstructor_Throws() {
        var exception = Assert.Throws<Exception>(
            () => ConfigFactory.CreateFromUrl<NoPublicConstructor>("custom://host/path"));

        Assert.Equal("Unable to create config instance from url: no constructor found.", exception.Message);
    }

    [Fact]
    public void ToUrl_GenericOverload_SerializesActualInstance() {
        var config = new TestConfig {
            Host = "example.com",
            Port = 1234,
            Secure = true
        };

        var url = ConfigFactory.ToUrl<TestConfig>("test", config);

        Assert.Equal("test://example.com:1234?secure=True", url);
    }

    [Fact]
    public void ToUrl_GenericAndNonGenericOverloads_AreEquivalent() {
        var config = new TestConfig {
            Host = "example.com",
            Port = 1234,
            Secure = true
        };

        var genericUrl = ConfigFactory.ToUrl<TestConfig>("test", config);
        var nonGenericUrl = ConfigFactory.ToUrl("test", config);

        Assert.Equal(nonGenericUrl, genericUrl);
    }

    [Fact]
    public void ToUrl_GenericOverload_UsesValuesFromEachInstance() {
        var configA = new TestConfig {
            Host = "first.example.com",
            Port = 1234,
            Secure = true
        };
        var configB = new TestConfig {
            Host = "second.example.com",
            Port = 5678,
            Secure = false
        };

        var urlA = ConfigFactory.ToUrl<TestConfig>("test", configA);
        var urlB = ConfigFactory.ToUrl<TestConfig>("test", configB);

        Assert.Equal("test://first.example.com:1234?secure=True", urlA);
        Assert.Equal("test://second.example.com:5678?secure=False", urlB);
        Assert.NotEqual(urlA, urlB);
    }

    private sealed class UrlConfig {
        public UrlConfig(
            string scheme,
            string path,
            string host,
            int port,
            string user,
            string password,
            int poolSize = 5,
            bool ssl = false) {
            Scheme = scheme;
            Path = path;
            Host = host;
            Port = port;
            User = user;
            Password = password;
            PoolSize = poolSize;
            Ssl = ssl;
        }

        public string Scheme { get; }
        public string Path { get; }
        public string Host { get; }
        public int Port { get; }
        public string User { get; }
        public string Password { get; }
        public int PoolSize { get; }
        public bool Ssl { get; }
    }

    private sealed class NoPublicConstructor {
        private NoPublicConstructor() { }
    }

    private sealed class TestConfig {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public bool Secure { get; set; }
    }
}
