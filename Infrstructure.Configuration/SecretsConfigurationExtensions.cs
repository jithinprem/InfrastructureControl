using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrstructure.Configuration;

public static class SecretsConfigurationExtensions
{
    public static IConfigurationBuilder AddInfisicalSecrets(this IConfigurationBuilder builder, string appsettingsPath = "appsettings.json")
    {
        // Load the appsettings.json file
        if (!File.Exists(appsettingsPath))
        {
            throw new FileNotFoundException($"Appsettings file not found: {appsettingsPath}");
        }

        var json = File.ReadAllText(appsettingsPath);

        // Fetch secrets from Infisical
        var secretsProvider = new SecretsProvider(new HttpClient { BaseAddress = new Uri("https://app.infisical.com") });
        var secrets = secretsProvider.FetchSecretsAsync().GetAwaiter().GetResult();

        // Replace placeholders
        var replacedJson = ReplacePlaceholders(json, secrets);

        // Add the replaced JSON as a configuration source
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(replacedJson));
        builder.AddJsonStream(stream);

        return builder;
    }

    private static string ReplacePlaceholders(string json, Dictionary<string, string> secrets)
    {
        // Regex to match {infisical.secrets.Key}
        var regex = new Regex(@"\{infisical\.secrets\.([^}]+)\}");

        return regex.Replace(json, match =>
        {
            var key = match.Groups[1].Value;
            if (secrets.TryGetValue(key, out var value))
            {
                // If the value is a JSON object or array (starts with { or [), do not replace to avoid JSON parsing issues
                // Instead, leave the placeholder and handle in code
                var trimmed = value.TrimStart();
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                {
                    return match.Value; // Leave placeholder
                }
                else
                {
                    return JsonSerializer.Serialize(value);
                }
            }
            else
            {
                throw new KeyNotFoundException($"Secret not found in Infisical: {key}");
            }
        });
    }
}
