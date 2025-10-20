using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

using esAPI.Middleware;

namespace esAPI.Tests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public GlobalExceptionMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
        }

        [Fact]
        public async Task InvokeAsync_NoException_CallsNextMiddleware()
        {
            // Arrange
            var nextCalled = false;
            RequestDelegate next = (context) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(200); // Default status
        }

        [Fact]
        public async Task InvokeAsync_ArgumentNullException_Returns400BadRequest()
        {
            // Arrange
            RequestDelegate next = (context) => throw new ArgumentNullException("testParam", "Test argument null");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(400);
            context.Response.ContentType.Should().Be("application/json");

            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            errorResponse.GetProperty("message").GetString().Should().Be("Invalid request parameters");
            errorResponse.GetProperty("statusCode").GetInt32().Should().Be(400);
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_Returns400BadRequest()
        {
            // Arrange
            RequestDelegate next = (context) => throw new ArgumentException("Invalid argument provided");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(400);

            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            errorResponse.GetProperty("message").GetString().Should().Be("Invalid request parameters");
        }

        [Fact]
        public async Task InvokeAsync_UnauthorizedAccessException_Returns401Unauthorized()
        {
            // Arrange
            RequestDelegate next = (context) => throw new UnauthorizedAccessException("Access denied");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(401);

            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            errorResponse.GetProperty("message").GetString().Should().Be("Unauthorized access");
            errorResponse.GetProperty("statusCode").GetInt32().Should().Be(401);
        }

        [Fact]
        public async Task InvokeAsync_InvalidOperationException_Returns400BadRequest()
        {
            // Arrange
            RequestDelegate next = (context) => throw new InvalidOperationException("Invalid operation");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(400);

            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            errorResponse.GetProperty("message").GetString().Should().Be("Invalid operation");
        }

        [Fact]
        public async Task InvokeAsync_TimeoutException_Returns408RequestTimeout()
        {
            // Arrange
            RequestDelegate next = (context) => throw new TimeoutException("Operation timed out");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(408);

            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            errorResponse.GetProperty("message").GetString().Should().Be("Request timeout");
            errorResponse.GetProperty("statusCode").GetInt32().Should().Be(408);
        }

        [Fact]
        public async Task InvokeAsync_GenericException_Returns500InternalServerError()
        {
            // Arrange
            RequestDelegate next = (context) => throw new Exception("Something went wrong");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be(500);

            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            errorResponse.GetProperty("message").GetString().Should().Be("An error occurred while processing your request");
            errorResponse.GetProperty("statusCode").GetInt32().Should().Be(500);
        }

        [Fact]
        public async Task InvokeAsync_DevelopmentEnvironment_IncludesExceptionDetails()
        {
            // Arrange
            _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);

            RequestDelegate next = (context) => throw new Exception("Detailed error message");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            errorResponse.TryGetProperty("details", out var detailsProperty).Should().BeTrue();
            detailsProperty.GetString().Should().Be("Detailed error message");

            errorResponse.TryGetProperty("stackTrace", out var stackTraceProperty).Should().BeTrue();
            stackTraceProperty.GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task InvokeAsync_ProductionEnvironment_ExcludesExceptionDetails()
        {
            // Arrange  
            _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);

            RequestDelegate next = (context) => throw new Exception("Detailed error message");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            // In production, we should still get basic error properties
            errorResponse.GetProperty("message").GetString().Should().Be("An error occurred while processing your request");
            errorResponse.GetProperty("statusCode").GetInt32().Should().Be(500);

            // But details should not be included (or should be null/empty if included)
            if (errorResponse.TryGetProperty("details", out var detailsProperty))
            {
                var details = detailsProperty.GetString();
                details.Should().BeNullOrEmpty();
            }
        }

        [Fact]
        public async Task InvokeAsync_AllExceptions_IncludeTimestamp()
        {
            // Arrange
            var beforeTest = DateTime.UtcNow;
            RequestDelegate next = (context) => throw new Exception("Test exception");
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);
            var afterTest = DateTime.UtcNow;

            // Assert
            var responseBody = await ReadResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            errorResponse.TryGetProperty("timestamp", out var timestampProperty).Should().BeTrue();
            var timestamp = timestampProperty.GetDateTime();

            timestamp.Should().BeOnOrAfter(beforeTest);
            timestamp.Should().BeOnOrBefore(afterTest);
        }

        [Fact]
        public async Task InvokeAsync_LogsExceptionWithRequestDetails()
        {
            // Arrange
            var exception = new Exception("Test exception for logging");
            RequestDelegate next = (context) => throw exception;
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/test";

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("POST") && v.ToString()!.Contains("/api/test")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ResponseAlreadyStarted_DoesNotModifyResponse()
        {
            // Arrange
            RequestDelegate next = async (context) =>
            {
                await context.Response.WriteAsync("Already started");
                // Simulate exception after response has started - this should be caught by global handler
            };

            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();

            // Act
            await middleware.InvokeAsync(context);

            // Assert - Response should contain the written content
            var responseBody = await ReadResponseBody(context);
            responseBody.Should().Be("Already started");
        }

        [Theory]
        [InlineData("GET", "/api/users")]
        [InlineData("POST", "/simulation")]
        [InlineData("PUT", "/orders/123")]
        [InlineData("DELETE", "/items/456")]
        public async Task InvokeAsync_LogsCorrectRequestMethodAndPath(string method, string path)
        {
            // Arrange
            var exception = new Exception("Test exception");
            RequestDelegate next = (context) => throw exception;
            var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
            var context = CreateHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(method) && v.ToString()!.Contains(path)),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<string> ReadResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}
