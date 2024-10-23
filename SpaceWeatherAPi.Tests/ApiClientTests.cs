using NUnit.Framework;
using System.Threading.Tasks;

[TestFixture]
public class ApiClientTests
{
    private IApiClient _apiClient;

    [SetUp]
    public void Setup()
    {
        // Initialize your API client here
        // You might want to use a mock HTTP client for unit testing
        _apiClient = new ApiClient(/* dependencies */);
    }

    [Test]
    public async Task GetDataAsync_ReturnsValidData()
    {
        // Arrange
        string dataType = "FLR";
        string startDate = "2024-01-01";
        string endDate = "2024-01-31";

        // Act
        var result = await _apiClient.GetDataAsync(dataType, startDate, endDate);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        // Add more specific assertions based on your expected data structure
    }

  
}