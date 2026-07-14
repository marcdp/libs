using System.ComponentModel.DataAnnotations;
using DProjects.Config.Attributes;

namespace DProjects.Config.Test;

public class ValidatorTests {
    [Fact]
    public void ValidateAndThrow_WithValidConfig_ReturnsSameInstance() {
        var config = new ValidatedConfig("production");

        var result = Validator.ValidateAndThrow(config);

        Assert.Same(config, result);
    }

    [Fact]
    public void ValidateAndThrow_WhenConstructorParameterIsInvalid_ThrowsValidationException() {
        var config = new ValidatedConfig("test");

        Assert.Throws<ValidationException>(() => Validator.ValidateAndThrow(config));
    }

    [Fact]
    public void ValidateAndThrow_WhenRequiredConstructorParameterIsNull_ThrowsValidationException() {
        var config = new ValidatedConfig(null);

        Assert.Throws<ValidationException>(() => Validator.ValidateAndThrow(config));
    }

    private sealed class ValidatedConfig {
        public ValidatedConfig(
            [Required, DProjects.Config.Attributes.AllowedValues("development", "production")] string? Name) {
            this.Name = Name;
        }

        public string? Name { get; }
    }
}
