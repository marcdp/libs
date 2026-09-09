using DProjects.Functional;

namespace DProjects.Functional.Tests {

    public class ResultTests {

        // methods
        [Fact]
        public void Ok_ExposesValueAndRunsOnlySuccessBranch() {
            var result = Result<int, Error>.Ok(21);
            var failureCalls = 0;

            var mapped = result.Match(value => value * 2, _ => { failureCalls++; return -1; });

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(21, result.Value);
            Assert.Equal(42, mapped);
            Assert.Equal(0, failureCalls);
            Assert.Throws<InvalidOperationException>(() => result.Error);
        }
        [Fact]
        public void Fail_ExposesErrorAndRunsOnlyFailureBranch() {
            var error = new Error("invalid", "Synthetic failure");
            var result = Result<int, Error>.Fail(error);
            var successCalls = 0;

            var mapped = result.Match(value => { successCalls++; return value; }, failure => failure.Code.Length);

            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Same(error, result.Error);
            Assert.Equal("invalid".Length, mapped);
            Assert.Equal(0, successCalls);
            Assert.Throws<InvalidOperationException>(() => result.Value);
        }
        [Fact]
        public void Match_RejectsEitherNullDelegateBeforeDispatch() {
            var success = Result<string, string>.Ok("value");
            var failure = Result<string, string>.Fail("error");

            Assert.Throws<ArgumentNullException>(() => success.Match<string>(null!, error => error));
            Assert.Throws<ArgumentNullException>(() => failure.Match(value => value, null!));
        }
        [Fact]
        public void NullPayloads_ArePreservedAsValidBranchValues() {
            var success = Result<string?, string?>.Ok(null);
            var failure = Result<string?, string?>.Fail(null);

            Assert.Null(success.Value);
            Assert.Null(failure.Error);
            Assert.Equal("success-null", success.Match(value => value == null ? "success-null" : "value", _ => "failure"));
            Assert.Equal("failure-null", failure.Match(_ => "success", error => error == null ? "failure-null" : "error"));
        }
        [Fact]
        public void Error_PreservesCodeAndDescription() {
            var error = new Error("code", "description");

            Assert.Equal("code", error.Code);
            Assert.Equal("description", error.Description);
        }
    }
}
