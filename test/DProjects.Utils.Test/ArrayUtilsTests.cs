using Xunit;
using DProjects.Utils;

namespace DProjects.Utils.Tests
{
    public class ArrayUtilsTests {
        [Fact()]
        public void IsArrayTest() {
            Assert.True(ArrayUtils.IsArray(new int[] { 1, 2, 3 }));
            Assert.True(ArrayUtils.IsArray(new object[] { 1, 2, 3 }));
            Assert.True(ArrayUtils.IsArray(new string[] { "1", "2", "3" }));
            Assert.False(ArrayUtils.IsArray(1));
        }
    }
}