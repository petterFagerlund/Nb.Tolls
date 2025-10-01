using System.Net;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nb.Tolls.WebApi.Host.ExceptionHandlers;
using Xunit;

namespace Nb.Tolls.WebApi.Host.UnitTests.ExceptionHandlers;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task Middleware_ReturnsBadRequest_ForArgumentException()
    {
        var context = new DefaultHttpContext();
        RequestDelegate next = _ => throw new ArgumentException("Invalid argument");
        var middleware = new GlobalExceptionHandlerMiddleware(next, A.Fake<ILogger<GlobalExceptionHandlerMiddleware>>());

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_ReturnsUnauthorized_ForUnauthorizedAccessException()
    {
        var context = new DefaultHttpContext();
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Access denied");
        var middleware = new GlobalExceptionHandlerMiddleware(next, A.Fake<ILogger<GlobalExceptionHandlerMiddleware>>());

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
    }

}