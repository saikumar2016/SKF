using Microsoft.Extensions.Configuration;
using SKF.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;



namespace SKF.Services
{
    
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _deploymentName;
        private readonly string _apiVersion;

        public OpenAIService(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _endpoint = config["OpenAI:Endpoint"];
            _apiKey = config["OpenAI:ApiKey"];
            _deploymentName = config["OpenAI:DeploymentName"];
            _apiVersion = config["OpenAI:ApiVersion"];
        }

        public ProductAttributeQuery ParseOpenAIResponse(string content)
        {
            // Remove markdown code block markers (```json, ```, and single backticks)
            string cleanedContent = Regex.Replace(content, @"^```json\s*|^```\s*|```$", "", RegexOptions.Multiline);
            cleanedContent = cleanedContent.Replace("`", "").Trim();

            // Optionally log for debugging
            Console.WriteLine("Cleaned content: " + cleanedContent);

            // Deserialize to model
            try
            {
                var result = JsonSerializer.Deserialize<ProductAttributeQuery>(cleanedContent);
                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Deserialization error: " + ex.Message);
                return null;
            }
        }

        public async Task<ProductAttributeQuery> ExtractQueryInfoAsync(string userQuery)
        {
            var prompt = $"Extract the product designation and attribute from: \"{userQuery}\". Respond in JSON: {{ \"Product\": \"...\", \"Attribute\": \"...\" }}";
            var requestBody = new
            {
                messages = new[]
                {
                new { role = "system", content = "You are a helpful assistant for SKF product datasheets." },
                new { role = "user", content = prompt }
            },
                max_tokens = 50,
                temperature = 0.0
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            var result = ParseOpenAIResponse(content);
            //var result = JsonSerializer.Deserialize<ProductAttributeQuery>(content);
            return result;
        }
    }
}
