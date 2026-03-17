using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class EnumUtilsTests {

        [Fact()]
        public void TryParseTest() {
            Assert.Equal(StringComparison.Ordinal, EnumUtils.TryParse<StringComparison>("Ordinal"));
            Assert.Equal(StringComparison.Ordinal, EnumUtils.TryParse<StringComparison>("ordinal"));
            Assert.Equal(StringComparison.Ordinal, EnumUtils.TryParse<StringComparison>("ORDINAL"));
        }

    }
}