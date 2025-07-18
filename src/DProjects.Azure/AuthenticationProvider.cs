using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DProjects.Azure {


    public class AuthenticationProvider {


        //variables
        private IConfidentialClientApplication mClientApplication;
        private DateTime mAuthorizationHeaderExpiresOn;  
        private string? mAuthorizationHeader;


        //properties
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string[] AppScopes { get; set; }
        public string TenantId { get; set; }


        //constructor
        public AuthenticationProvider(string clientId, string clientSecret, string[] appScopes, string tenantId) {
            ClientId = clientId;
            ClientSecret = clientSecret;
            AppScopes = appScopes;
            TenantId = tenantId;
            mClientApplication = ConfidentialClientApplicationBuilder.Create(ClientId)
                .WithClientSecret(ClientSecret)
                .WithClientId(ClientId)  
                .WithTenantId(TenantId)
                .Build();
        }

        //methods
        public async Task AuthenticateRequestAsync(HttpRequestMessage request) {
            if (mAuthorizationHeader == null || mAuthorizationHeaderExpiresOn < DateTime.Now.AddMinutes(5)) {
                var result = await mClientApplication.AcquireTokenForClient(AppScopes).ExecuteAsync();
                mAuthorizationHeader = result.CreateAuthorizationHeader();
                var dt = result.ExpiresOn;
                mAuthorizationHeaderExpiresOn = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Utc), TimeZoneInfo.Local);
            }
            request.Headers.Add("Authorization", mAuthorizationHeader);
        }
        //public void AuthenticateRequest(HttpRequestMessage request) {
        //    if (mAuthorizationHeader == null || mAuthorizationHeaderExpiresOn < DateTime.Now.AddMinutes(5)) {
        //        var result = mClientApplication.AcquireTokenForClient(AppScopes).ExecuteAsync().Result;
        //        mAuthorizationHeader = result.CreateAuthorizationHeader();
        //        var dt = result.ExpiresOn;
        //        mAuthorizationHeaderExpiresOn = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Utc), TimeZoneInfo.Local);
        //    }
        //    request.Headers.Add("Authorization", mAuthorizationHeader);
        //}
    }

}


