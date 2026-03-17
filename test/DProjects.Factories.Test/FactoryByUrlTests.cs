using Xunit;
using DProjects.Factories;
using DProjects.Factories.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using DProjects.Secrets;

namespace DProjects.Factories.Tests
{
    public class FactoryByUrlTests : Base {

        //inner classes
        //inner classes
        public interface ISomething {
            string GetName();
            string GetPassword();
        }
        //something1
        public class Something1 : ISomething {
            public string GetName() => "1";
            public string GetPassword() => "";
        }
        [Protocol("something1", "")]
        public class Something1Factory : IFactoryByUrl<ISomething> {
            public ISomething Create(string url) {
                return new Something1();
            }
        }
        //something2
        public class Something2 : ISomething {
            public string GetName() => "2";
            public string GetPassword() => "";
        }
        [Protocol("something2", "")]
        public class Something2Factory : IFactoryByUrl<ISomething> {
            public ISomething Create(string url) {
                return new Something2();
            }
        }
        //dir
        public class SomethingDir : ISomething {
            public string GetName() => "dir";
            public string GetPassword() => "";
        }
        [Protocol("dir", "")]
        public class SomethingDirFactory : IFactoryByUrl<ISomething> {
            public ISomething Create(string url) {
                return new SomethingDir();
            }
        }
        //passwored
        public class SomethingPasswored(string pass) : ISomething {
            public string GetName() => "password";
            public string GetPassword() => pass;
        }
        [Protocol("passwored", "")]
        public class SomethingPassworedFactory : IFactoryByUrl<ISomething> {
            public ISomething Create(string url) {
                var aUrl = new System.Uri(url);
                return new SomethingPasswored(aUrl.UserInfo.Split(":")[1]);
            }
        }
        //default
        public class SomethingDefault : ISomething {
            public string GetName() => "default";
            public string GetPassword() => "";
        }

        //secret provider
        public class SecretProvider : ISecretProvider {
            public Secret? Get(string name) {
                return new Secret(name, "", "1234");
            }
            public Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
                return Task.FromResult<Secret?>(new Secret(name, "", "1234"));
            }
        }

        //tests
        //[Theory()]
        //[InlineData("something1:", "1")]
        //[InlineData("something2:", "2")]
        //[InlineData("dir://my-path", "dir")]
        //public void Create_ShouldPrependDirToUrl_WhenUrlStartsWithSlash(string url, string expected) {
        //    var instance = mFactoryByUrl.Create(url);
        //    var result = instance.GetName();
        //    Assert.Equal(expected, result);
        //}

        [Theory()]
        [InlineData("111", "1")]
        [InlineData("222", "2")]
        [InlineData("333", "dir")]
        public void Create_ShouldReplaceUrlWithAliasValue_WhenUrlMatchesAlias(string url, string expected) {
            var instance = mFactoryByUrl.Create(url);
            var result = instance.GetName();
            Assert.Equal(expected, result);
        }

        [Theory()]
        [InlineData("something1", "1")]
        [InlineData("something2", "2")]
        public void Create_ShouldAppendColonToUrl_WhenUrlDoesNotContainColon(string url, string expected) {
            var instance = mFactoryByUrl.Create(url);
            var result = instance.GetName();
            Assert.Equal(expected, result);
        }

        [Theory()]
        [InlineData("passwored://user:${secret:my_secret}@host/path", "1234")]
        public void Create_ShouldFillPassword_WhenUrlContainsUserInfoButNoPassword(string url, string expected) {
            var instance = mFactoryByUrl.Create(url);
            var result = instance.GetPassword();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Create_ShouldReturnDefaultInstance_WhenUrlIsEmpty() {
            var instance = mFactoryByUrl.Create("");
            var result = instance.GetName();
            Assert.Equal("default", result);
        }

        [Fact]
        public void Create_ShouldThrowArgumentException_WhenSchemeNotFoundInProtocols() {
            Assert.Throws<ArgumentException>(() => mFactoryByUrl.Create("unknown://test"));
        }
    }
}
