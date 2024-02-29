using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class ZipUtilsTests {


        [Fact()]
        public void ZipFileTest() {
            var tempFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempFile = System.IO.Path.Combine(tempFolder, "test.txt");
            var tempZipFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            System.IO.Directory.CreateDirectory(tempFolder);
            System.IO.File.WriteAllText(tempFile, "test");
            Assert.True(System.IO.File.Exists(tempFile));
            Assert.True(System.IO.File.Exists(tempFile));
            ZipUtils.ZipFile(tempFile, tempZipFile);
            Assert.True(ZipUtils.HasEntry(tempZipFile, "test.txt"));
            System.IO.File.Delete(tempFile);
            System.IO.Directory.Delete(tempFolder);
            System.IO.File.Delete(tempZipFile);
        }

        [Fact()]
        public void HasEntryTest() {
            var tempFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempFile = System.IO.Path.Combine(tempFolder, "test.txt");
            var tempZipFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            System.IO.Directory.CreateDirectory(tempFolder);
            System.IO.File.WriteAllText(tempFile, "test");
            Assert.True(System.IO.File.Exists(tempFile));
            Assert.True(System.IO.File.Exists(tempFile));
            ZipUtils.ZipFile(tempFile, tempZipFile);
            Assert.True(ZipUtils.HasEntry(tempZipFile, "test.txt"));
            Assert.False(ZipUtils.HasEntry(tempZipFile, "test123.txt"));
            System.IO.File.Delete(tempFile);
            System.IO.Directory.Delete(tempFolder);
            System.IO.File.Delete(tempZipFile);
        }

        [Fact()]
        public void UnZipTest() {
            // write the test method to create a zip file, and then unzip that file to test if method XmlUtils.Unzip works as expected            
            var tempFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempFile = System.IO.Path.Combine(tempFolder, "test.txt");
            var tempZipFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            System.IO.Directory.CreateDirectory(tempFolder);
            System.IO.File.WriteAllText(tempFile, "test");
            Assert.True(System.IO.File.Exists(tempFile));
            Assert.True(System.IO.File.Exists(tempFile));
            ZipUtils.ZipFile(tempFile, tempZipFile);
            Assert.True(ZipUtils.HasEntry(tempZipFile, "test.txt"));
            ZipUtils.UnZip(tempFolder, tempZipFile);
            Assert.True(System.IO.File.Exists(tempFile));
            System.IO.File.Delete(tempFile);
            System.IO.Directory.Delete(tempFolder);
            System.IO.File.Delete(tempZipFile);
        }

        [Fact()]
        public void GetZipListTest() {
            var tempFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempFile = System.IO.Path.Combine(tempFolder, "test.txt");
            var tempZipFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            System.IO.Directory.CreateDirectory(tempFolder);
            System.IO.File.WriteAllText(tempFile, "test");
            Assert.True(System.IO.File.Exists(tempFile));
            Assert.True(System.IO.File.Exists(tempFile));
            ZipUtils.ZipFile(tempFile, tempZipFile);
            Assert.True(ZipUtils.HasEntry(tempZipFile, "test.txt"));
            var zipList = ZipUtils.GetZipList(tempZipFile);
            Assert.Contains("test.txt", zipList);
            System.IO.File.Delete(tempFile);
            System.IO.Directory.Delete(tempFolder);
            System.IO.File.Delete(tempZipFile);
        }
    }
}