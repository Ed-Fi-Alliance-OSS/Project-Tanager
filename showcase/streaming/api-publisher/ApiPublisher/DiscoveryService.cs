using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiPublisher
{
    public class DiscoveryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _discoveryApiUrl;

        public DiscoveryService(HttpClient httpClient, string discoveryApiUrl)
        {
            _httpClient = httpClient;
            _discoveryApiUrl = discoveryApiUrl;
        }

        public async Task<DiscoveryApiResponse> GetApiUrlsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_discoveryApiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var discoveryResponse = JsonSerializer.Deserialize<DiscoveryApiResponse>(content);

                if (discoveryResponse == null)
                {
                    throw new InvalidOperationException("Failed to deserialize discovery API response");
                }

                return discoveryResponse;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve API URLs from discovery endpoint: {ex.Message}", ex);
            }
        }
    }
}
