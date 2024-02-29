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
        [InlineData("Hello world", "H4sIAAAAAAAACvNIzcnJVyjPL8pJAQBSntaLCwAAAA==")]
        public void GzipTest(string text, string result) {
            Assert.Equal(result, Base64Utils.ToBase64(GzipUtils.Gzip(text)));
        }

    }
}