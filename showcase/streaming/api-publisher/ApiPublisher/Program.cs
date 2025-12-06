using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace ApiPublisher
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Starting API Publisher...");

            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var discoveryApiUrl = configuration["DiscoveryApiUrl"];
            var clientId = configuration["OAuth:ClientId"];
            var clientSecret = configuration["OAuth:ClientSecret"];
            var bootstrapServers = configuration["Kafka:BootstrapServers"];
            var groupId = configuration["Kafka:GroupId"];
            var topic = configuration["Kafka:Topic"];
            var autoOffsetReset = configuration["Kafka:AutoOffsetReset"];

            if (string.IsNullOrEmpty(discoveryApiUrl) || string.IsNullOrEmpty(clientId) || 
                string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(bootstrapServers))
            {
                Console.WriteLine("Error: Missing required configuration values");
                Environment.Exit(1);
            }

            // Initialize HTTP client
            var httpClient = new HttpClient();

            try
            {
                // Get API URLs from Discovery API
                Console.WriteLine($"Connecting to Discovery API: {discoveryApiUrl}");
                var discoveryService = new DiscoveryService(httpClient, discoveryApiUrl!);
                var apiUrls = await discoveryService.GetApiUrlsAsync();

                Console.WriteLine($"OAuth URL: {apiUrls.Urls.OAuth}");
                Console.WriteLine($"Data Management API URL: {apiUrls.Urls.DataManagementApi}");

                // Authenticate with OAuth
                Console.WriteLine("Authenticating with OAuth...");
                var oauthService = new OAuthService(httpClient, clientId!, clientSecret!);
                var accessToken = await oauthService.GetAccessTokenAsync(apiUrls.Urls.OAuth);
                Console.WriteLine("Successfully authenticated");

                // Initialize API Publisher Service
                var publisherService = new ApiPublisherService(httpClient, apiUrls.Urls.DataManagementApi);
                publisherService.SetAccessToken(accessToken);

                // Configure Kafka Consumer
                var config = new ConsumerConfig
                {
                    BootstrapServers = bootstrapServers,
                    GroupId = groupId,
                    AutoOffsetReset = autoOffsetReset == "earliest" ? AutoOffsetReset.Earliest : AutoOffsetReset.Latest,
                    EnableAutoCommit = true
                };

                Console.WriteLine($"Connecting to Kafka: {bootstrapServers}");
                Console.WriteLine($"Subscribing to topic: {topic}");

                using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
                {
                    consumer.Subscribe(topic);

                    var cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (_, e) =>
                    {
                        e.Cancel = true;
                        cts.Cancel();
                    };

                    Console.WriteLine("Listening for messages... Press Ctrl+C to exit");

                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cts.Token);
                                
                                if (consumeResult?.Message?.Value != null)
                                {
                                    Console.WriteLine($"\nReceived message at offset {consumeResult.Offset.Value}");
                                    
                                    var message = JsonSerializer.Deserialize<KafkaMessage>(consumeResult.Message.Value);
                                    
                                    if (message != null)
                                    {
                                        Console.WriteLine($"Resource: {message.ProjectName}/{message.ResourceName}");
                                        await publisherService.PublishDocumentAsync(message);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Failed to deserialize message");
                                    }
                                }
                            }
                            catch (ConsumeException ex)
                            {
                                Console.WriteLine($"Consume error: {ex.Error.Reason}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing message: {ex.Message}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("\nShutting down...");
                    }
                    finally
                    {
                        consumer.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                httpClient.Dispose();
            }

            Console.WriteLine("API Publisher stopped");
        }
    }
}
