using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Text;

namespace BarcoCtrlApi
{
    public class BarcoCtrlApiClient
    {
        private readonly string _baseUrl;
        private readonly OAuth2Client _oauthClient;
        private TokenResponse _tokenResponse = null;
        private readonly HttpClient _httpClient;

        public BarcoCtrlApiClient(string baseUrl, OAuth2Client oauthClient)
        {
            if (!baseUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The base URL must use HTTPS", nameof(baseUrl));

            _baseUrl = baseUrl;
            _oauthClient = oauthClient;
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (HttpRequestMessage httpRequestMessage, X509Certificate2 certificate, X509Chain certificateChain, SslPolicyErrors sslPolicyErrors) =>
                {
                    return true; // Ignore SSL certificate errors
                };
            _httpClient = new HttpClient(handler);
        }

        public BarcoCtrlApiClient(string baseUrl, OAuth2Client oauthClient, TokenResponse token)
        {
            if (!baseUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The base URL must use HTTPS", nameof(baseUrl));
            this._tokenResponse = token;
            this._baseUrl = baseUrl;
            this._oauthClient = oauthClient;
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (HttpRequestMessage httpRequestMessage, X509Certificate2 certificate, X509Chain certificateChain, SslPolicyErrors sslPolicyErrors) =>
                {
                    return true; // Ignore SSL certificate errors
                };
            _httpClient = new HttpClient(handler);
        }

        // Get a valid token before each request, with exception handling
        private async Task EnsureValidTokenAsync()
        {
            try
            {
                if (_tokenResponse == null)
                {
                    _tokenResponse = await _oauthClient.GetAccessTokenAsync();
                }
                else if (_tokenResponse.ExpirationTime <= DateTime.UtcNow.AddMinutes(-1)) // Renew 1 min before expiration )
                {
                    _tokenResponse = await _oauthClient.GetAccessTokenAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error ensuring valid token: {ex.Message} {ex.InnerException}", ex);
            }
        }

        // Example: Get API Version with error handling
        public async Task<string> GetApiVersionAsync()
        {
            try
            {
                await EnsureValidTokenAsync();
                var handler = new HttpClientHandler();

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                var response = await _httpClient.GetAsync($"{_baseUrl}/version");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var versionData = JsonConvert.DeserializeObject<ApiVersionResponse>(json);
                return versionData.Data.Version;
        
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error calling GetApiVersionAsync: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error in GetApiVersionAsync: {ex.Message}", ex);
            }
        }


        // Example: Get All Workplaces with error handling
        public async Task<List<WorkplaceDto>> GetWorkplacesAsync()
        {
            try
            {
                await EnsureValidTokenAsync();
                var handler = new HttpClientHandler();

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                var response = await _httpClient.GetAsync($"{_baseUrl}/workplaces");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                // Deserialize JSON into the WorkplacesResponse model
                var workplacesData = JsonConvert.DeserializeObject<WorkplacesResponse>(json);
                return workplacesData.Data;  // Return the list of WorkplaceDTO
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error calling GetWorkplacesAsync: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error in GetWorkplacesAsync: {ex.Message}", ex);
            }
        }


        // Other API methods with similar try-catch error handling

        public async Task<WorkplaceDto> GetWorkplaceAsync(string id)
        {
            try
            {
                await EnsureValidTokenAsync();

                _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                var response = await _httpClient.GetAsync($"{_baseUrl}/workplaces/{id}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var workplaceData = JsonConvert.DeserializeObject<WorkplaceResponse>(json);
                return workplaceData.Data;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error calling GetWorkplaceAsync for ID {id}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error in GetWorkplaceAsync for ID {id}: {ex.Message}", ex);
            }
        }

        public async Task<List<SourceDto>> GetSourcesAsync()
        {
            try
            {
                await EnsureValidTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/sources");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var sourcesData = JsonConvert.DeserializeObject<SourcesResponse>(json);
                return sourcesData.Data;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error calling GetSourcesAsync: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error in GetSourcesAsync: {ex.Message}", ex);
            }
        }

        public async Task<List<VisualObjectDto>> GetWorkplaceContentAsync(string workplaceId)
        {
            try
            {
                await EnsureValidTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                // Fetch the content (sources) displayed on the workplace
                var response = await _httpClient.GetAsync($"{_baseUrl}/workplaces/{workplaceId}/content");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                // Deserialize into a list of VisualObjectDTO
                var contentData = JsonConvert.DeserializeObject<ContentResponse>(json);
                return contentData.Data; // Return the list of visual objects
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error calling GetWorkplaceContentAsync for workplace {workplaceId}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error in GetWorkplaceContentAsync for workplace {workplaceId}: {ex.Message}", ex);
            }
        }

        public async Task<List<VisualObjectDto>> PutWorkplaceContentAsync(string workplaceId, List<VisualObjectDto> toUpdate)
        {
            try
            {
                await EnsureValidTokenAsync();

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                // Fetch the content (sources) displayed on the workplace
                    
                var response = await _httpClient.PutAsync($"{_baseUrl}/workplaces/{workplaceId}/content", new StringContent(JsonConvert.SerializeObject(toUpdate), Encoding.UTF8, "application/json"));                    
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                // Deserialize into a list of VisualObjectDTO
                var contentData = JsonConvert.DeserializeObject<ContentResponse>(json);
                return contentData.Data; // Return the list of visual objects
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error calling PutWorkplaceContentAsync for workplace {workplaceId}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error in PutWorkplaceContentAsync for workplace {workplaceId}: {ex.Message}", ex);
            }
        }

        public async Task<int> DeleteWorkplaceContentAsync(string workplaceId, List<string> toDelete)
        {
            try
            {
                await EnsureValidTokenAsync();


                _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                // Fetch the content (sources) displayed on the workplace
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"{_baseUrl}/workplaces/{workplaceId}/content"),
                    Content = new StringContent(JsonConvert.SerializeObject(toDelete), Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                return 1;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error calling PutWorkplaceContentAsync for workplace {workplaceId}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error in PutWorkplaceContentAsync for workplace {workplaceId}: {ex.Message}", ex);
            }            
        }
    }

    public class VisualObjectDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("geometry")]
        public GeometryPX Geometry { get; set; }

        [JsonProperty("window", NullValueHandling = NullValueHandling.Ignore)]
        public WindowProperties Window { get; set; }

        [JsonProperty("content")]
        public SourceContentDto Content { get; set; }
    }

    public class SourceContentDto
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("options")]
        public ContentOptions Options { get; set; }
    }

    public class ContentOptions
    {
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("zoom")]
        public int Zoom { get; set; }

        [JsonProperty("pointOfInterest")]
        public PointOfInterest PointOfInterest { get; set; }

        [JsonProperty("audio")]
        public AudioSettings Audio { get; set; }

        [JsonProperty("showLabel")]
        public bool ShowLabel { get; set; }
    }

    public class GeometryPX
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class WindowProperties
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("showFrame")]
        public bool ShowFrame { get; set; }
    }

    public class PointOfInterest
    {
        [JsonProperty("top")]
        public int Top { get; set; }

        [JsonProperty("left")]
        public int Left { get; set; }
    }

    public class AudioSettings
    {
        [JsonProperty("mute")]
        public bool Mute { get; set; }

        [JsonProperty("volume")]
        public int Volume { get; set; }
    }

    // Data Models (same as before)

    public class ApiVersionResponse
    {
        [JsonProperty("data")]
        public VersionData Data { get; set; }
    }

    public class VersionData
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class WorkplacesResponse
    {
        [JsonProperty("data")]
        public List<WorkplaceDto> Data { get; set; }
    }

    public class WorkplaceResponse
    {
        [JsonProperty("data")]
        public WorkplaceDto Data { get; set; }
    }

    public class ContentResponse
    {
        [JsonProperty("data")]
        public List<VisualObjectDto> Data { get; set; }
    }

    // The Workplace data model (DTO)
    public class WorkplaceDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("wallGeometry", NullValueHandling = NullValueHandling.Ignore)]
        public WallGeometry WallGeometry { get; set; }
    }

    // Wall geometry for workplaces of type "Wall"
    public class WallGeometry
    {
        [JsonProperty("sizePx")]
        public SizePx SizePx { get; set; }

        [JsonProperty("grid")]
        public Grid Grid { get; set; }
    }

    // Dimensions of the wall in pixels
    public class SizePx
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    // Grid structure of the wall
    public class Grid
    {
        [JsonProperty("columns")]
        public int Columns { get; set; }

        [JsonProperty("rows")]
        public int Rows { get; set; }
    }

    public class SourcesResponse
    {
        [JsonProperty("data")]
        public List<SourceDto> Data { get; set; }
    }

    public class SourceDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("audio")]
        public SourceAudioSettings Audio { get; set; }

        [JsonProperty("interactivity")]
        public InteractivitySettings Interactivity { get; set; }

        [JsonProperty("streams")]
        public List<StreamDto> Streams { get; set; }
    }

    public class SourceAudioSettings
    {
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; }
    }

    public class InteractivitySettings
    {
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; }
    }

    public class StreamDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
