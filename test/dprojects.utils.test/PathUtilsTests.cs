using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DProjects.Utils.Tests {
    public class PathUtilsTests {


        [Theory()]
        [InlineData("/a", "b", "/a/b")]
        [InlineData("/a", "..", "/")]
        [InlineData("/a/b", "..", "/a")]
        [InlineData("/a/b", "../c", "/a/c")]
        [InlineData("", "/c", "/c")]
        [InlineData("/", "/c/.", "/c")]
        [InlineData("/", "..", "/")]
        [InlineData("/", "../..", "/")]
        [InlineData("/a", "./././././.", "/a")]
        public void CombineTest(string path1, string path2, string result) {
            Assert.Equal(result, PathUtils.Combine(path1, path2));
        }

        [Theory()]
        [InlineData("/a/././b", "/a/b")]
        [InlineData("/a/../b/../c/.", "/c")]
        public void NormalizeTest(string path, string result) {
            Assert.Equal(result, PathUtils.Normalize(path));
        }

        [Theory()]
        [InlineData("/a", "/a/b", "/b")]
        [InlineData("/a/b", "/a", "/a")]
        public void UncombineTest(string path1, string path2, string result) {
            Assert.Equal(result, PathUtils.Uncombine(path1, path2));
        }


        [Theory()]
        [InlineData("/a", "b", "/a/b")]
        [InlineData("/a", "/b", "/b")]
        public void CreateTest(string pwd, string path, string result) {
            Assert.Equal(result, PathUtils.Create(pwd, path));
        }

        [Theory()]
        [InlineData("/", "/")]
        [InlineData("/a", "/")]
        [InlineData("/a/b", "/a")]
        [InlineData("/a/b/.", "/a")]
        public void GetPathParentTest(string path, string result) {
            Assert.Equal(result, PathUtils.GetPathParent(path));
        }


        [Theory()]
        [InlineData("/", "/")]
        [InlineData("/a", "/")]
        [InlineData("/a/b", "/")]
        [InlineData("/a/b/c/.", "/a")]
        public void GetPathGrandParentTest(string path, string result) {
            Assert.Equal(result, PathUtils.GetPathGrandParent(path));
        }

        [Theory()]
        [InlineData("/", 1, "/")]
        [InlineData("/a", 1, "/")]
        [InlineData("/a/b", 1, "/a")]
        [InlineData("/a/b", 2, "/")]
        [InlineData("/a/b/c/.", 2, "/a")]
        public void GetPathAncestorTest(string path, int level, string result) {
            Assert.Equal(result, PathUtils.GetPathAncestor(path, level));
        }

        [Theory()]
        [InlineData("/", "")]
        [InlineData("/file.test", ".test")]
        [InlineData("/file.test/subfile", "")]
        [InlineData("/file.test/subfile.txt", ".txt")]
        public void GetPathExtensionTest(string path, string result) {
            Assert.Equal(result, PathUtils.GetPathExtension(path));
        }


        [Theory()]
        [InlineData("/", "")]
        [InlineData("/file.test", "file.test")]
        [InlineData("/./file.test", "file.test")]
        [InlineData("/file.test/subfile", "subfile")]
        [InlineData("/file.test/subfile.txt", "subfile.txt")]
        [InlineData("/a/b/..", "a")]
        [InlineData("/a/b/.", "b")]
        public void GetPathNameTest(string path, string result) {
            Assert.Equal(result, PathUtils.GetPathName(path));
        }

        [Theory()]
        [InlineData("/", "")]
        [InlineData("/file.test", "file")]
        [InlineData("/./file.test", "file")]
        [InlineData("/file.test/subfile", "subfile")]
        [InlineData("/file.test/subfile.txt", "subfile")]
        [InlineData("/a/b/..", "a")]
        [InlineData("/a/b/.", "b")]
        public void GetPathNameWithoutExtensionTest(string path, string result) {
            Assert.Equal(result, PathUtils.GetPathNameWithoutExtension(path));
        }


        [Theory()]
        [InlineData("/", "")]
        [InlineData("/file.test", "file.test")]
        [InlineData("/./file.test", "file.test")]
        [InlineData("/file.test/subfile", "file.test")]
        [InlineData("/file.test/subfile.txt", "file.test")]
        [InlineData("/a/b/..", "a")]
        [InlineData("/a/b/.", "a")]
        public void GetPathFirstNameTest(string path, string result) {
            Assert.Equal(result, PathUtils.GetPathFirstName(path));
        }


        [Theory()]
        [InlineData("/", 0)]
        [InlineData("/file.test", 1)]
        [InlineData("/./file.test", 1)]
        [InlineData("/file.test/subfile", 2)]
        [InlineData("/file.test/subfile.txt", 2)]
        [InlineData("/a/b/..", 1)]
        [InlineData("/a/b/.", 2)]
        public void GetPathPartsCountTest(string path, int result) {
            Assert.Equal(result, PathUtils.GetPathPartsCount(path));
        }

        [Theory()]
        [InlineData("/", 0, "/")]
        [InlineData("/a/b/..", 1, "/a")]
        [InlineData("/a/b/c/d/e", 1, "/a")]
        public void GetPathCuttedByLevelTest(string path, int level, string result) {
            Assert.Equal(result, PathUtils.GetPathCuttedByLevel(path, level));
        }


        [Theory()]
        [InlineData("/", 0, "/")]
        [InlineData("/a/b/..", 1, "/")]
        [InlineData("/a/b/c/d/e", 1, "/b/c/d/e")]
        [InlineData("/a/b/c/d/e", 2, "/c/d/e")]
        [InlineData("/a/b/../c/d/e", 2, "/d/e")]
        public void GetPathCuttedFromLevelTest(string path, int level, string result) {
            Assert.Equal(result, PathUtils.GetPathCuttedFromLevel(path, level));
        }

        [Theory()]
        [InlineData('\\')]
        [InlineData(':')]
        [InlineData('*')]
        [InlineData('?')]
        [InlineData('<')]
        [InlineData('>')]
        [InlineData('|')]
        [InlineData('\"')]
        public void GetPathInvalidCharsReplacedTest(char aChar) {
            var path = "/hello/world" + aChar;
            var result = "/hello/world_";
            Assert.Equal(result, PathUtils.GetPathInvalidCharsReplaced(path));
        }

        [Theory()]
        [InlineData('\\')]
        [InlineData(':')]
        [InlineData('*')]
        [InlineData('?')]
        [InlineData('<')]
        [InlineData('>')]
        [InlineData('|')]
        [InlineData('\"')]
        [InlineData(',')]
        [InlineData('/')]
        public void GetPathInvalidCharsReplacedStrongTest(char aChar) {
            var path = "/hello/world" + aChar;
            var result = "helloworld";
            Assert.Equal(result, PathUtils.GetPathInvalidCharsReplacedStrong(path));
        }


        [Theory()]
        [InlineData("/path1", "path1")]
        [InlineData("/pathà", "path")]
        [InlineData("/path<", "path")]
        public void GetPathInvalidCharsReplacedStrongStrongTest(string path, string result) {
            Assert.Equal(result, PathUtils.GetPathInvalidCharsReplacedStrongStrong(path));
        }

        [Theory()]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData("", typeof(ArgumentException))]
        [InlineData("hello/path", typeof(ArgumentException))]
        [InlineData("hello/path/", typeof(ArgumentException))]
        [InlineData("/path1", null)]
        public void ValidateTest(string path, Type? exceptionType) {
            if (exceptionType == null) {
                PathUtils.Validate(path);
            } else {  
                Assert.Throws(exceptionType, () => PathUtils.Validate(path));
            }
        }


        [Theory()]
        [InlineData("/path/to/file#[]", "/path/to/file%23%5B%5D")]
        public void GetPathURLEncodedTest(string path, string result) {
            Assert.Equal(result, PathUtils.GetPathURLEncoded(path));
        }


        [Theory()]
        [InlineData("/a", "/a", 0)]
        [InlineData("/a", "/A", 32)]
        [InlineData("/A", "/A", 0)]
        [InlineData("/A", "/B", -1)]
        public void CompareNameTest(string name1, string name2, int result) {
            Assert.Equal(result, PathUtils.CompareName(name1, name2));
        }


        [Theory()]
        [InlineData("/a", "/a", 0)]
        [InlineData("/a", "/A", 32)]
        [InlineData("/A", "/A", 0)]
        [InlineData("/A", "/B", -1)]
        public void ComparePathTest(string path1, string path2, int result) {
            Assert.Equal(result, PathUtils.ComparePath(path1, path2));
        }
    }
}