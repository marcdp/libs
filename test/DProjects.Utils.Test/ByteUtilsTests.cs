using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class ByteUtilsTests {


        [Theory()]
        [InlineData(new byte[] { 1,2,3}, new byte[] { 4,5,6}, false)]
        [InlineData(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }, true)]
        [InlineData(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3, 4 }, false)]
        public void CompareTest(byte[] buffer1, byte[] buffer2, bool result) {
            Assert.Equal(result, ByteUtils.Compare(buffer1, buffer2));
        }

        [Theory()]
        [InlineData(new byte[] { 1, 2, 3 }, new byte[] { 2, 3 }, 1)]
        [InlineData(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }, 0)]
        [InlineData(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3, 4 }, -1)]
        public void IndexOfTest(byte[] buffer1, byte[] buffer2, int index) {
            Assert.Equal(index, ByteUtils.IndexOf(buffer1, buffer2));
        }

        [Theory()]
        [InlineData(new byte[] { 1, 2, 3 }, new byte[] { 2, 3 }, new byte[] { 1, 2, 3, 2, 3 })]
        public void ConcatTest(byte[] buffer1, byte[] buffer2, byte[] result) {
            Assert.Equal(result, ByteUtils.Concat(buffer1, buffer2));
        }
    }
}