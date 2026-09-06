using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class ProcessUtilsTests {


        [Fact()]
        public async Task ExecuteCmdAsyncTest() {
            if (EnvironmentUtils.IsWindows()) {
                Assert.Contains("Microsoft Windows", await ProcessUtils.ExecuteCmdAsync("ver", TestContext.Current.CancellationToken));
            }
        }
        [Fact()]
        public async Task ExecuteBashAsyncTest() {
            if (EnvironmentUtils.IsLinux()) {
                Assert.Contains("Linux", await ProcessUtils.ExecuteCmdAsync("uname", TestContext.Current.CancellationToken));
            }
        }
        [Fact()]
        public void ExecuteProcessAsyncTest() {
        }
    }
}