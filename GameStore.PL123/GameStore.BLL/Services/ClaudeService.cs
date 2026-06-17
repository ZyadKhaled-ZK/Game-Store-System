using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GameStore.BLL.Services
{
    public class ClaudeService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public ClaudeService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;

            var baseUrl = _config["Claude:BaseUrl"] ?? "https://api.anthropic.com/";
            _http.BaseAddress = new Uri(baseUrl);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config["Claude:ApiKey"]);
        }

        public async Task<string> AskAsync(string prompt)
        {
            var body = new
            {
                model = _config["Claude:Model"] ?? "claude-sonnet-4-5-20250929",
                max_tokens = 500,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(body);

            var response = await _http.PostAsync(
                "/v1/messages",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return ParseResponse(result);
            }

            return $"Error: {result}";
        }

        private string ParseResponse(string jsonResponse)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(jsonResponse);
                var text = jsonDoc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "No response";
            }
            catch
            {
                return jsonResponse;
            }
        }
    }
}
