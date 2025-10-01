using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FakeItEasy;
using Nb.Tolls.Domain.Enums;
using Nb.Tolls.Domain.Results;
using Nb.Tolls.WebApi.IntegrationTests.Integration;
using Nb.Tolls.WebApi.Models.Requests;
using Nb.Tolls.WebApi.Models.Responses;
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
            VehicleType = Vehicle.Car, TollTimes = new[] { new DateTimeOffset(2025, 8, 2, 8, 0, 0, TimeSpan.Zero) }
        };

        var applicationResult = new ApplicationResult<DailyTollFeesResult>
        {
            Result = new DailyTollFeesResult
            {
                TollFees =
                [
                    new TollFeeResult() { TollFeeTime = request.TollTimes.First().UtcDateTime, TollFee = 30, }
                ]
            }
        };

        A.CallTo(() => _hostSutApplication.TollFeesService.GetTollFees(request.VehicleType, request.TollTimes))
            .Returns(Task.FromResult(applicationResult));

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
        var tollFeeResponse = JsonSerializer.Deserialize<TollFeesResponse>(responseString, _jsonSerializerOptions);

        Assert.NotNull(tollFeeResponse);
        Assert.Equal(applicationResult.Result.TollFees.First().TollFee, tollFeeResponse.TollFees.First().TollFee);
        Assert.Equal(
            DateOnly.FromDateTime(applicationResult.Result.TollFees.First().TollFeeTime),
            tollFeeResponse.TollFees.First().TollDate);
    }
}