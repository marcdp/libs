using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections;
using System.IO;

namespace DProjects.Utils.Tests {


    public class AuthUtilsTests {

        [Theory()]
        [InlineData("john@example.com","abc123", "Basic am9obkBleGFtcGxlLmNvbTphYmMxMjM=")]
        public void CreateBasicTest(string user, string pass, string result) {
            Assert.Equal(result, AuthUtils.CreateBasic(new NetworkCredential(user, pass)));
        }

        [Theory()]
        [InlineData("john@example.com", "abc123", "GET", "/", "?", "","2020-01-01", null, "hmac am9obkBleGFtcGxlLmNvbTpxVjVkaUUrN3BxUUk5azNDbmdCVlcwTmQzRXVHOGtjU0NpMDdQdWpJZm5NPQ==")]
        [InlineData("john@example.com", "abc123", "GET", "/", "?var1=123&var2=fsdfasdf", "", "2020-01-01", "2024-01-01T16:12:34.345", "hmac am9obkBleGFtcGxlLmNvbTpLdU9Zanl0RXk2bFA4RU5LZitaWE00UGgzSzJLdFVndVMrK0k1U2hBOXgwPQ==")]
        public void CreateHmacTest(string user, string pass, string method, string path, string query, string contentType, string dateHeader, string dateHeaderToUse, string result) {
            Assert.Equal(result, AuthUtils.CreateHmac(new NetworkCredential(user, pass), method, path, query, contentType, dateHeader != null ? DateTime.Parse(dateHeader) : default, dateHeaderToUse != null ? DateTime.Parse(dateHeaderToUse) : default));
        }
    }

}