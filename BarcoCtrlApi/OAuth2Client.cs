using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BarcoCtrlApi
{
    public class OAuth2Client
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tokenUrl;
        HttpClientHandler handler = new HttpClientHandler();


        public OAuth2Client(string clientId, string clientSecret, string tokenUrl)
        {
            if (!tokenUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The token URL must use HTTPS", nameof(tokenUrl));
           
            this._clientId = clientId;
            this._clientSecret = clientSecret;
            this._tokenUrl = tokenUrl;
        }

        public async Task<TokenResponse> GetAccessTokenAsync()
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback =
                    (HttpRequestMessage httpRequestMessage, X509Certificate2 certificate, X509Chain certificateChain, SslPolicyErrors sslPolicyErrors) =>
                    {
                        return true; // Ignore SSL certificate errors
                    };
                using (HttpClient client = new HttpClient(handler))
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl);
                    var requestBody = new StringContent(
                        $"grant_type=client_credentials&client_id={_clientId}&client_secret={_clientSecret}",
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded"
                    );

                    request.Content = requestBody;

                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(content);
                        tokenResponse.ExpirationTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                        return tokenResponse;
                    }
                    else
                    {
                        throw new HttpRequestException($"Error retrieving access token: {response.StatusCode} {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve access token: {ex.Message}", ex);
            }
        }
    }

    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        public DateTime ExpirationTime { get; set; }
    }
}
