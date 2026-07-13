using DProjects.Config.Attributes;

namespace DProjects.Config.Test;

public class AllowedValuesAttributeTests {
    private readonly AllowedValuesAttribute _attribute = new("red", "green", 3);

    [Fact]
    public void Values_ContainsConfiguredValues() {
        Assert.Equal(new object[] { "red", "green", 3 }, _attribute.Values);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("green")]
    [InlineData(3)]
    public void IsValid_WithAllowedValue_ReturnsTrue(object value) {
        Assert.True(_attribute.IsValid(value));
    }

    [Fact]
    public void IsValid_WithNull_ReturnsTrue() {
        Assert.True(_attribute.IsValid(null));
    }

    [Theory]
    [InlineData("blue")]
    [InlineData(4)]
    public void IsValid_WithDisallowedValue_ReturnsFalse(object value) {
        Assert.False(_attribute.IsValid(value));
    }
}
