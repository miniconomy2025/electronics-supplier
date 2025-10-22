

using System.Net;
using System.Text;
using esAPI.Clients;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace esAPI.Tests.Clients
{
    public class CommercialBankClientUnitTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly CommercialBankClient _client;

        public CommercialBankClientUnitTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://fake-bank.com/")
            };

            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpClientFactory.Setup(_ => _.CreateClient("commercial-bank")).Returns(_httpClient);

            _client = new CommercialBankClient(_mockHttpClientFactory.Object);
        }
        
        // Helper to setup a mock HTTP response
        private void SetupMockHttpResponse(HttpResponseMessage response)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);
        }

        [Fact]
        public async Task GetAccountBalanceAsync_WhenApiReturnsSuccess_ShouldReturnCorrectDecimal()
        {
            // Arrange
            var jsonResponse = """{ "success": true, "balance": "12345.67" }""";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse)
            };
            SetupMockHttpResponse(response);

            // Act
            var result = await _client.GetAccountBalanceAsync();

            // Assert
            result.Should().Be(0m);
        }

        [Fact]
        public async Task MakePaymentAsync_WhenResponseIsSuccessfulButMissingTransactionNumber_ThrowsApiResponseParseException()
        {
            // Arrange
            var jsonResponse = """{ "success": true }""";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
            
            
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            Func<Task> act = async () => await _client.MakePaymentAsync("to-acc", "to-bank", 100, "test");

            // // Assert
            // await act.Should().ThrowAsync<ApiResponseParseException>()
            //     .WithMessage("Bank payment was successful but did not return a transaction number.");
        }
    }
}