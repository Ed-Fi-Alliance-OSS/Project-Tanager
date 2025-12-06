using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiPublisher
{
    public class ApiPublisherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _dataManagementApiUrl;
        private string _accessToken = string.Empty;

        public ApiPublisherService(HttpClient httpClient, string dataManagementApiUrl)
        {
            _httpClient = httpClient;
            _dataManagementApiUrl = dataManagementApiUrl;
        }

        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
        }

        public async Task<bool> PublishDocumentAsync(KafkaMessage message)
        {
            try
            {
                // Skip deleted documents
                if (message.Deleted.ToLower() == "true")
                {
                    Console.WriteLine($"Skipping deleted document: {message.ResourceName}");
                    return true;
                }

                // Remove the 'id' field from edfidoc
                var edfiDocJson = message.EdfiDoc.GetRawText();
                var edfiDocObject = JsonSerializer.Deserialize<JsonDocument>(edfiDocJson);
                
                if (edfiDocObject == null)
                {
                    throw new InvalidOperationException("Failed to deserialize edfidoc");
                }

                // Create a new JSON object without the 'id' field
                var jsonObject = new System.Text.Json.Nodes.JsonObject();
                foreach (var property in edfiDocObject.RootElement.EnumerateObject())
                {
                    if (property.Name != "id")
                    {
                        jsonObject.Add(property.Name, System.Text.Json.Nodes.JsonNode.Parse(property.Value.GetRawText()));
                    }
                }

                var payload = jsonObject.ToJsonString();

                // Build the API endpoint URL
                var resourceName = message.ResourceName;
                var projectName = message.ProjectName.ToLower();
                var endpoint = $"{_dataManagementApiUrl}/{projectName}/{resourceName}s";

                // Create the HTTP request
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                // Send the request
                Console.WriteLine($"Publishing {resourceName} to {endpoint}");
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully published {resourceName}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to publish {resourceName}: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing document: {ex.Message}");
                return false;
            }
        }
    }
}
