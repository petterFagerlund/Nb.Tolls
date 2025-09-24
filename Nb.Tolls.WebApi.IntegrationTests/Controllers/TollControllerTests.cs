using System.Text.Json;
using System.Text.Json.Serialization;
using Nb.Tolls.WebApi.IntegrationTests.Integration;
using Xunit.Abstractions;

namespace Nb.Tolls.WebApi.IntegrationTests.Controllers;

public class TollControllerTests
{
    private readonly HostSutApplication _hostSutApplication;
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() }
    };

    public TollControllerTests(HostSutApplication hostSutApplication, ITestOutputHelper outputHelper)
    {
        _hostSutApplication = hostSutApplication;
        hostSutApplication.Output = outputHelper;
        hostSutApplication.InitApplication();
        _httpClient = hostSutApplication.Client;
    }
}