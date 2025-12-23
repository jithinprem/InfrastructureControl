using System.Net.Http.Json;
using System.Text.Json;

namespace Infrstructure.Configuration;

public class SecretsProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceToken;
    private readonly string _projectId;
    private readonly string _environment;

    public SecretsProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://app.infisical.com");
        _serviceToken = Environment.GetEnvironmentVariable("INFISICAL_SERVICE_TOKEN") ?? throw new InvalidOperationException("INFISICAL_SERVICE_TOKEN environment variable is not set");
        _projectId = Environment.GetEnvironmentVariable("INFISICAL_PROJECT_ID") ?? throw new InvalidOperationException("INFISICAL_PROJECT_ID environment variable is not set");
        _environment = Environment.GetEnvironmentVariable("INFISICAL_ENVIRONMENT") ?? "dev";
    }

    public async Task<Dictionary<string, string>> FetchSecretsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v4/secrets?environment={_environment}&workspaceId={_projectId}");
        request.Headers.Add("Authorization", $"Bearer {_serviceToken}");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<InfisicalResponse>();
        var secrets = new Dictionary<string, string>();
        if (responseData?.Secrets != null)
        {
            foreach (var secret in responseData.Secrets.Where(s => !string.IsNullOrEmpty(s.SecretKey)))
            {
                if (!secrets.ContainsKey(secret.SecretKey))
                {
                    secrets[secret.SecretKey] = secret.SecretValue;
                }
                // If duplicate, skip (or log if needed)
            }
        }
        return secrets;
    }

    public async Task<string> GetSecretAsync(string key)
    {
        var secrets = await FetchSecretsAsync();
        if (secrets.TryGetValue(key, out var value))
        {
            return value;
        }
        throw new KeyNotFoundException($"Secret not found: {key}");
    }

    private class InfisicalResponse
    {
        public List<InfisicalSecret> Secrets { get; set; } = new List<InfisicalSecret>();
    }

    private class InfisicalSecret
    {
        public string SecretKey { get; set; } = string.Empty;
        public string SecretValue { get; set; } = string.Empty;
    }
}
