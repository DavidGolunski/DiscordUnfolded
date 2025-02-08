using BarRaider.SdTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DiscordUnfolded {
    public static class DiscordOAuth2 {
        private static readonly HttpClient httpClient = new HttpClient();

        public static string ExchangeCode(string code, string clientID, string clientSecret, string redirectURI) {
            var values = new Dictionary<string, string>
            {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectURI }
        };

            var content = new FormUrlEncodedContent(values);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/v10/oauth2/token") {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientID}:{clientSecret}")));

            var response = httpClient.SendAsync(request).GetAwaiter().GetResult();

            if(!response.IsSuccessStatusCode) {
                throw new Exception($"Error exchanging code: {response.StatusCode}");
            }

            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Received Oauth2 Response: " + responseBody);
            var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);
            
            return responseData["access_token"];
        }
    }
}
