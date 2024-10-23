
using System.Net;
using System.Text.Json;

namespace SpaceWeatherApi.Tests;
[TestFixture]
public class ApiClientTests : BaseTestFixture
{
    // *** FetchDONKIDataAsync Tests *** \\
    [Test, Description("Should return valid data")]
    public async Task FetchDONKIDataAsync_ReturnsValidData()
    {
        // Arrange
        string endpoint = "FLR";
        DateTime startDate = new(2023, 1, 1);
        DateTime endDate = new(2024, 1, 31);

        // Act
        var result = await ApiClient.FetchDONKIDataAsync<object>(endpoint, startDate, endDate);

        // Assert
        Assert.DoesNotThrow(() =>
        {
            var jsonString = JsonSerializer.Serialize(result);
            var jsonDocument = JsonDocument.Parse(jsonString);

            Assert.Multiple(() =>
            {
                Assert.That(jsonDocument.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array),
                            "Root element should be a JSON array");

                Assert.That(jsonDocument.RootElement.GetArrayLength(), Is.GreaterThan(0),
                    "JSON array should contain at least one element");
            });
        }, "Data should be valid JSON");
    }


    [TestCase("bad", Description = "Should return 404 status for invalid endpoint")]
    [TestCase("", Description = "Should return 404 status for blank endpoint")]
        public void FetchDONKIDataAsync_ThrowsHttpRequestException_ForInvalidOrBlankEndpoint(string invalidEndpoint)
    {
        // Arrange
        DateTime startDate = new(2024, 1, 1);
        DateTime endDate = new(2024, 1, 31);

        // Act & Assert
        Assert.That(async () => await ApiClient.FetchDONKIDataAsync<object>(invalidEndpoint, startDate, endDate),
            Throws.TypeOf<HttpRequestException>()
                .With.Property("StatusCode").EqualTo(HttpStatusCode.NotFound));
    }


    // *** GetDataAsync Tests *** \\
    [TestCase("today", Description = "Should return data for today")]
    [TestCase("yr1", Description = "Should return for the past number year")]
    public async Task GetDataAsync_ShouldGiveDataBack_ForDateShortHandsAsync(string shorthand)
    {
        // Arrange
        string endDate = DateTime.UtcNow.AddDays(+2).ToString();
        string endpoint = "FLR";

        // Act
        var result = await ApiClient.GetDataAsync(endpoint, shorthand, endDate);

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
    }

}






