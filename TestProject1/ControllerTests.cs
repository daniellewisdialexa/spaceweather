using Microsoft.AspNetCore.Mvc;
using Moq;
using SpaceWeatherApi.Controllers;
using SpaceWeatherApi.Utils;
using System.Text.Json;

namespace SpaceWeatherApi.Tests;
[TestFixture]
public class ControllerTests : BaseTestFixture {
    [Test]
    public async Task CorrelationControllerSameTime_ReturnsValidData()
    {

        //Arrange
        var controller = new CorrelationController(ApiClient, FlareAnalyzerService, DataUtils);

        // Act
        var result = await controller.GetCorrelationSameTimeEvents("2023-01-01", "2023-01-31");

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>(), "Result should be an OkObjectResult");

        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.Not.Null, "OkObjectResult Value should not be null");

        Assert.DoesNotThrow(() =>
        {
            JsonSerializer.Serialize(okResult.Value);
        }, "The content should be serializable to JSON");
    }


    [Test]
    public async Task GetCorrelationSameTimeEvents_ApiClientThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var mockApiClient = new Mock<IApiClient>();
        var mockDataUtils = new Mock<DataUtils>();

        // Configure the mock ApiClient to throw an exception when GetDataAsync is called
        mockApiClient.Setup(client => client.GetDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                     .ThrowsAsync(new Exception("Simulated network error"));

        var controller = new CorrelationController(mockApiClient.Object,FlareAnalyzerService, mockDataUtils.Object);

        // Act
        var result = await controller.GetCorrelationSameTimeEvents("2023-01-01", "2023-01-31");

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(500)); // Internal Server Error
        Assert.That(objectResult.Value, Is.EqualTo("An error occurred while processing your request."));
    }
}


