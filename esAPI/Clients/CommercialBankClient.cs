using esAPI.Exceptions;

namespace esAPI.Clients
{
    public interface ICommercialBankClient
    {
        Task<decimal> GetAccountBalanceAsync();
        Task<HttpResponseMessage> CreateAccountAsync(object requestBody);
        Task<string> MakePaymentAsync(string toAccountNumber, string toBankName, decimal amount, string description);
        Task<string?> RequestLoanAsync(decimal amount);
        Task<string?> GetAccountDetailsAsync();
        Task<HttpResponseMessage> GetAccountAsync();
    }

    public class CommercialBankClient(IHttpClientFactory factory) : BaseClient(factory, ClientName), ICommercialBankClient
    {
        private const string ClientName = "commercial-bank";

        public async Task<decimal> GetAccountBalanceAsync()
        {
            Console.WriteLine($"🔧 CommercialBankClient: Checking account balance...");
            var response = await _client.GetAsync("api/account/me/balance");
            Console.WriteLine($"🔧 CommercialBankClient: Balance response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ CommercialBankClient: Failed to get balance. Status: {response.StatusCode}");
                return 0m;
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"🔧 CommercialBankClient: Balance response content: {content}");

            using var doc = System.Text.Json.JsonDocument.Parse(content);

            // Check if the response indicates success
            if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                if (doc.RootElement.TryGetProperty("balance", out var balanceProp))
                {
                    // Handle balance as string (e.g., "60000000.00") and parse to decimal
                    var balanceString = balanceProp.GetString();
                    if (decimal.TryParse(balanceString, out var balance))
                    {
                        Console.WriteLine($"✅ CommercialBankClient: Account balance: {balance}");
                        return balance;
                    }
                    else
                    {
                        Console.WriteLine($"❌ CommercialBankClient: Failed to parse balance string: {balanceString}");
                        return 0m;
                    }
                }
                else
                {
                    Console.WriteLine($"❌ CommercialBankClient: Success response but no balance found");
                    return 0m;
                }
            }
            else
            {
                Console.WriteLine($"❌ CommercialBankClient: Balance request failed - success field is false or missing");
                return 0m;
            }
        }

        public async Task<HttpResponseMessage> CreateAccountAsync(object requestBody)
        {
            // Console.WriteLine($"🔧 CommercialBankClient: Making POST request to /account");
            // Console.WriteLine($"🔧 CommercialBankClient: Base address: {_client.BaseAddress}");
            // Console.WriteLine($"🔧 CommercialBankClient: Full URL: {_client.BaseAddress}/account");
            // Console.WriteLine($"🔧 CommercialBankClient: Request URI: {_client.BaseAddress}/account");

            try
            {
                // Use the full URL directly to ensure it's correct
                var fullUrl = $"{_client.BaseAddress}api/account";
                Console.WriteLine($"🔧 CommercialBankClient: Using full URL: {fullUrl}");

                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                Console.WriteLine($"🔧 CommercialBankClient: Request body: {json}");
                Console.WriteLine($"🔧 CommercialBankClient: Sending request with client certificate...");

                var response = await _client.SendAsync(request);
                Console.WriteLine($"🔧 CommercialBankClient: Response status: {response.StatusCode}");
                Console.WriteLine($"🔧 CommercialBankClient: Response URL: {response.RequestMessage?.RequestUri}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CommercialBankClient: Exception during POST: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"❌ CommercialBankClient: Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        public async Task<string?> RequestLoanAsync(decimal amount)
        {
            var requestBody = new BankLoanRequest { Amount = amount };
            var loanResponse = await PostAsync<BankLoanRequest, BankLoanResponse>("api/loan", requestBody);
            if (loanResponse.Success)
            {
                if (loanResponse.LoanNumber != null)
                {
                    var loanNumber = loanResponse.LoanNumber;
                    Console.WriteLine($"✅ CommercialBankClient: Loan request successful! Loan number: {loanNumber}");
                    return loanNumber;
                }
                else
                {
                    Console.WriteLine($"❌ CommercialBankClient: Success response but no loan_number found");
                    return null;
                }
            }
            else
            {
                // Handle failure response with detailed error information
                var errorMessage = loanResponse.Error ?? "Unknown error";
                var amountRemaining = loanResponse.AmountRemaining ?? 0;

                Console.WriteLine($"❌ CommercialBankClient: Loan request failed - Error: {errorMessage}, Amount remaining: {amountRemaining}");

                // If the loan was too large, we could potentially retry with the remaining amount
                if (errorMessage == "loanTooLarge" && amountRemaining > 0)
                {
                    Console.WriteLine($"💡 CommercialBankClient: Loan was too large. Remaining amount available: {amountRemaining}");
                }

                return null;
            }
           
        }

        public async Task<string> MakePaymentAsync(string toAccountNumber, string toBankName, decimal amount, string description)
        {
            var paymentReq = new BankPaymentRequest
            {
                ToAccountNumber = toAccountNumber,
                ToBankName = toBankName,
                Amount = amount,
                Description = description
            };

            try
            {
                var response = await _client.PostAsJsonAsync("api/transaction", paymentReq);
                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }

                var paymentResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();
                if (paymentResponse == null)
                {
                    throw new ApiResponseParseException("Failed to parse payment response from the bank.");
                }


                if (!paymentResponse.Success)
                {
                    var errorMessage = paymentResponse.Error ?? "Unknown payment error from bank.";
                    throw new ApiCallFailedException(errorMessage, response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                if (string.IsNullOrEmpty(paymentResponse.TransactionNumber))
                {
                    throw new ApiResponseParseException("Bank payment was successful but did not return a transaction number.");
                }
                return paymentResponse.TransactionNumber;
            }
            catch (Exception ex) when (ex is not ApiClientException and not ApiResponseParseException)
            {
                throw;
            }
        }

        public async Task<string?> GetAccountDetailsAsync()
        {
            var response = await _client.GetAsync("api/account/me");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("account_number", out var accNum) ? accNum.GetString() : null;
        }

        public async Task<HttpResponseMessage> GetAccountAsync()
        {
            return await _client.GetAsync("api/account/me");
        }
    }
}
