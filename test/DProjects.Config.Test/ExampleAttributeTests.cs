using DProjects.Config.Attributes;

namespace DProjects.Config.Test;

public class ExampleAttributeTests {
    [Fact]
    public void Constructor_SetsValueAndDescription() {
        var attribute = new ExampleAttribute("server.example.com", "Server address");

        Assert.Equal("server.example.com", attribute.Value);
        Assert.Equal("Server address", attribute.Description);
    }

    [Fact]
    public void Constructor_WithoutDescription_UsesEmptyDescription() {
        var attribute = new ExampleAttribute("server.example.com");

        Assert.Equal(string.Empty, attribute.Description);
    }
}
