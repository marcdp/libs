using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {


    public class GzipUtilsTests {

        [Theory]
        [InlineData("Hello world")]
        public void GzipTest(string text) {
            var compressed = GzipUtils.Gzip(text);
            var decompressed = Encoding.UTF8.GetString(GzipUtils.UnGzip(compressed));

            Assert.Equal(text, decompressed);
        }

    }
}
