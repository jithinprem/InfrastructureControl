using Infrstructure.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace SecretsFetcherTest;

public class SecretsFetcherTest
{
    private readonly ITestOutputHelper _output;

    public SecretsFetcherTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task FetchAndDisplayTopSecret()
    {
        // Arrange
        var httpClient = new HttpClient();
        var secretsProvider = new SecretsProvider(httpClient);

        // Act
        var secrets = await secretsProvider.FetchSecretsAsync();

        // Assert
        Assert.NotNull(secrets);
        Assert.NotEmpty(secrets);

        // Display the top one (first in dictionary)
        var firstSecret = secrets.First();
        _output.WriteLine($"Top secret: Key={firstSecret.Key}, Value={firstSecret.Value}");

        // Additional check: ensure the value is not null or empty
        Assert.False(string.IsNullOrEmpty(firstSecret.Value));
    }
}
