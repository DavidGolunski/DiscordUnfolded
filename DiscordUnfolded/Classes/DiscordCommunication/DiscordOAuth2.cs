using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordUnfolded.DiscordCommunication {
    public static class DiscordOAuth2 {
        private static readonly HttpClient httpClient = new HttpClient();

        public static string ExchangeCode(string code, string clientID, string clientSecret, string redirectURI, CancellationToken cancellationToken) {
            if(code == null || clientID == null || clientSecret == null || redirectURI == null) 
                return null;

            var values = new Dictionary<string, string>{
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

            var response = httpClient.SendAsync(request, cancellationToken).GetAwaiter().GetResult();

            if(!response.IsSuccessStatusCode) {
                throw new Exception($"Error exchanging code: {response.StatusCode}");
            }

            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Received Oauth2 Response: " + responseBody);
            var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);
            
            return responseData["access_token"];
        }

        // refresh a non-expired token
        public static string RefreshToken(string token, string clientID, string clientSecret, String[] scopes, CancellationToken cancellationToken) {
            if(token == null || clientID == null || clientSecret == null)
                return null;

            var values = new Dictionary<string, string>
            {
                { "client_id", clientID },
                { "client_secret", clientSecret },
                { "refresh_token", token },
                { "scope", string.Join(" ", scopes) },
                { "grant_type", "refresh_token" }
            };

            var content = new FormUrlEncodedContent(values);
            var requestUrl = "https://discord.com/api/v10/oauth2/token";

            var response = httpClient.PostAsync(requestUrl, content).GetAwaiter().GetResult();

            if(!response.IsSuccessStatusCode) {
                throw new Exception($"Error exchanging code: {response.StatusCode}" + " -- " + response.RequestMessage.ToString());
            }
            Console.WriteLine("Successfully refreshed token");
            string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            JObject responseJObject = JObject.Parse(responseBody);

            Logger.Instance.LogMessage(TracingLevel.INFO, responseBody.ToString());


            return responseJObject["token"]?.ToString();
            /*
            var values = new Dictionary<string, string>{
                { "grant_type", "refresh_token" },
                { "refresh_token", token }
            };

            var content = new FormUrlEncodedContent(values);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/v10/oauth2/token") {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientID}:{clientSecret}")));

            var response = httpClient.SendAsync(request, cancellationToken).GetAwaiter().GetResult();

            if(!response.IsSuccessStatusCode) {
                throw new Exception($"Error exchanging code: {response.StatusCode}" + " -- " + response.RequestMessage.ToString());
            }

            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Received Oauth2 Refresh Response: " + responseBody);
            var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);

            return responseData["access_token"];
            */
        }



        // revoke the rights from an existing token
        public static void RevokeToken(string token, string clientID, string clientSecret, CancellationToken cancellationToken) {
            if(token == null || clientID == null || clientSecret == null)
                return;

            var values = new Dictionary<string, string>{
                { "token", token },
                { "token_type_hint", "access_token" }
            };

            var content = new FormUrlEncodedContent(values);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/v10/oauth2/token/revoke") {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientID}:{clientSecret}")));

            var response = httpClient.SendAsync(request, cancellationToken).GetAwaiter().GetResult();

            if(!response.IsSuccessStatusCode) {
                throw new Exception($"Error exchanging code: {response.StatusCode}");
            }

        }
    }
}
