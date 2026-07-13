using DProjects.Config.Attributes;

namespace DProjects.Config.Test;

public class MinimumAttributeTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_SetsMinimumAndInclusivity(bool inclusive) {
        var attribute = new MinimumAttribute(2.5, inclusive);

        Assert.Equal(2.5, attribute.Min);
        Assert.Equal(inclusive, attribute.Inclusive);
    }

    [Fact]
    public void Constructor_UsesInclusiveMinimumByDefault() {
        var attribute = new MinimumAttribute(2.5);

        Assert.True(attribute.Inclusive);
    }

    [Fact]
    public void IsValid_IsNotImplemented() {
        var attribute = new MinimumAttribute(2.5);

        Assert.Throws<NotImplementedException>(() => attribute.IsValid(3));
    }
}
