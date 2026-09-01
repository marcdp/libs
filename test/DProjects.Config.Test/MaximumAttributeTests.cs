using DProjects.Config.Attributes;

namespace DProjects.Config.Test;

public class MaximumAttributeTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_SetsMaximumAndInclusivity(bool inclusive) {
        var attribute = new MaximumAttribute(9.5, inclusive);

        Assert.Equal(9.5, attribute.Max);
        Assert.Equal(inclusive, attribute.Inclusive);
    }

    [Fact]
    public void Constructor_UsesInclusiveMaximumByDefault() {
        var attribute = new MaximumAttribute(9.5);

        Assert.True(attribute.Inclusive);
    }

}
