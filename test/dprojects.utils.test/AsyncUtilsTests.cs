using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DProjects.Utils.Tests {
    public class AsyncUtilsTests {

        [Theory()]
        [InlineData(1)]
        [InlineData(false)]
        [InlineData(true)]
        [InlineData("sample value")]
        public void RunSyncTest(object value) {
            Assert.Equal(value, AsyncUtils.RunSync(async () => await Task.FromResult(value)));
        }


        public static int[] Data => [1,2,3,4,5,6,7,8,9];
        [Fact()]
        public async Task ToAsyncEnumerableTest() {
            var index = 0;
            await foreach (var item in Data.ToAsyncEnumerable()) {
                Assert.Equal(Data[index++], item);
            }
        }
        [Fact()]
        public async Task ToArrayAsyncTest() {
            var index = 0;
            foreach (var item in await Data.ToAsyncEnumerable().ToArrayAsync()) {
                Assert.Equal(Data[index++], item);
            }
        }
        [Fact()]
        public async Task ToListAsyncTest() {
            var index = 0;
            foreach (var item in await Data.ToAsyncEnumerable().ToListAsync()) {
                Assert.Equal(Data[index++], item);
            }
        }
        [Fact()]
        public void ToEnumerableTest() {
            var index = 0;
            foreach (var item in Data.ToAsyncEnumerable().ToEnumerable()) {
                Assert.Equal(Data[index++], item);
            }
        }
    }
}