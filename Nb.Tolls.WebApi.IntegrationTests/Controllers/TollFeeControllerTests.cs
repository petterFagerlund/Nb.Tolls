using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FakeItEasy;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;
using Nb.Tolls.WebApi.Host.Requests;
using Nb.Tolls.WebApi.Host.Responses;
using Nb.Tolls.WebApi.IntegrationTests.Integration;
using Xunit;
using Xunit.Abstractions;

namespace Nb.Tolls.WebApi.IntegrationTests.Controllers;

[Collection("API collection")]
public class TollFeeControllerTests
{
    private readonly HostSutApplication _hostSutApplication;
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TollFeeControllerTests(HostSutApplication hostSutApplication, ITestOutputHelper outputHelper)
    {
        _hostSutApplication = hostSutApplication;
        hostSutApplication.Output = outputHelper;
        hostSutApplication.InitApplication();
        _httpClient = hostSutApplication.Client;
    }

    [Fact]
    public async Task GetTollFee_Returns_200WithExpectedResponse()
    {
        var request = new TollFeesRequest
        {
            VehicleType = Vehicle.Car,
            TollTimes =
            [
                new DateTimeOffset(2025, 9, 30, 8, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 30, 9, 45, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 30, 11, 14, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 30, 16, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 30, 17, 16, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 30, 12, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 29, 5, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 29, 8, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 29, 12, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 29, 15, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 29, 16, 16, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2025, 9, 26, 16, 16, 0, TimeSpan.FromHours(2))
            ]
        };

        var applicationResult = new ApplicationResult<List<TollFeeResult>>
        {
            Result = new List<TollFeeResult>
            {
                new() { TollFeeTime = new DateTime(2025, 9, 26), TollFee = 18 },
                new() { TollFeeTime = new DateTime(2025, 9, 29), TollFee = 52 },
                new() { TollFeeTime = new DateTime(2025, 9, 30), TollFee = 60 }
            }
        };
        A.CallTo(() => _hostSutApplication.TollFeesCalculatorService.CalculateTollFees(request.VehicleType, request.TollTimes))
            .Returns(applicationResult);

        const string requestUri = "/api/tollfee";
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        var json = JsonSerializer.Serialize(request, jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        var tollFeeResponse = JsonSerializer.Deserialize<List<TollFeeResponse>>(responseString, _jsonSerializerOptions);

        Assert.NotNull(tollFeeResponse);
        Assert.Equal(3, tollFeeResponse.Count);

        Assert.Equal(new DateOnly(2025, 9, 26), tollFeeResponse[0].TollDate);
        Assert.Equal(18, tollFeeResponse[0].TollFee);

        Assert.Equal(new DateOnly(2025, 9, 29), tollFeeResponse[1].TollDate);
        Assert.Equal(52, tollFeeResponse[1].TollFee);

        Assert.Equal(new DateOnly(2025, 9, 30), tollFeeResponse[2].TollDate);
        Assert.Equal(60, tollFeeResponse[2].TollFee);
    }

    [Fact]
    public async Task GetTollFee_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new TollFeesRequest
        {
            VehicleType = Vehicle.Car,
            TollTimes =
            [
                new DateTimeOffset(2025, 9, 30, 8, 15, 0, TimeSpan.FromHours(2))
            ]
        };

        A.CallTo(() => _hostSutApplication.TollFeesCalculatorService.CalculateTollFees(request.VehicleType, request.TollTimes))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        const string requestUri = "/api/tollfee";
        var json = JsonSerializer.Serialize(request, _jsonSerializerOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync(requestUri, content);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var responseString = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

        Assert.Equal("Something went wrong", jsonResponse.GetProperty("error").GetProperty("message").GetString());
    }
}