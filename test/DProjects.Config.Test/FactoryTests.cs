namespace DProjects.Config.Test;

public class FactoryTests {
    [Fact]
    public void CreateFromUrl_PopulatesUriPartsAndConvertsQueryParameters() {
        var config = Factory.CreateFromUrl<UrlConfig>(
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
        var config = Factory.CreateFromUrl<UrlConfig>(
            "postgres://marc:secret@db.example.com/main");

        Assert.Equal(5, config.PoolSize);
        Assert.False(config.Ssl);
    }

    [Fact]
    public void CreateFromUrl_WhenTypeHasNoPublicConstructor_Throws() {
        var exception = Assert.Throws<Exception>(
            () => Factory.CreateFromUrl<NoPublicConstructor>("custom://host/path"));

        Assert.Equal("Unable to create config instance from url: no constructor found.", exception.Message);
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
}
