using System.Text.Json;

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

    public class CommercialBankClient(IHttpClientFactory factory) : ICommercialBankClient
    {
        private readonly IHttpClientFactory _factory = factory;
        private readonly HttpClient _client = factory.CreateClient("commercial-bank");

        public async Task<decimal> GetAccountBalanceAsync()
        {
            Console.WriteLine($"[CommercialBankClient] Checking account balance");
            var fullUrl = $"{_client.BaseAddress}/account/me/balance";
            Console.WriteLine($"[CommercialBankClient] GetBalance using full URL: {fullUrl}");
            var response = await _client.GetAsync(fullUrl);
            Console.WriteLine($"[CommercialBankClient] Balance response status: {response.StatusCode}");

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
                    decimal balance = 0m;
                    
                    // Handle balance as either number or string
                    if (balanceProp.ValueKind == JsonValueKind.Number)
                    {
                        balance = balanceProp.GetDecimal();
                        Console.WriteLine($"✅ CommercialBankClient: Account balance (number): {balance}");
                        return balance;
                    }
                    else if (balanceProp.ValueKind == JsonValueKind.String)
                    {
                        var balanceString = balanceProp.GetString();
                        if (decimal.TryParse(balanceString, out balance))
                        {
                            Console.WriteLine($"✅ CommercialBankClient: Account balance (string): {balance}");
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
                        Console.WriteLine($"❌ CommercialBankClient: Balance property has unexpected type: {balanceProp.ValueKind}");
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
            try
            {
                // Use the correct endpoint (base address already includes /api)
                var fullUrl = $"{_client.BaseAddress}/account";
                Console.WriteLine($"[CommercialBankClient] Using full URL: {fullUrl}");

                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                var json = JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                Console.WriteLine($"[CommercialBankClient] Request body: {json}");

                var response = await _client.SendAsync(request);
                Console.WriteLine($"[CommercialBankClient] Response status: {response.StatusCode}");
                Console.WriteLine($"[CommercialBankClient] Response URL: {response.RequestMessage?.RequestUri}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CommercialBankClient] Exception during POST: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[CommercialBankClient] Inner exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        public async Task<string?> RequestLoanAsync(decimal amount)
        {
            var requestBody = new { amount };

            Console.WriteLine($"[CommercialBankClient] Requesting loan of {amount}");
            Console.WriteLine($"[CommercialBankClient] Request body: {JsonSerializer.Serialize(requestBody)}");
            Console.WriteLine($"[CommercialBankClient] Base address: {_client.BaseAddress}");
            var fullLoanUrl = $"{_client.BaseAddress}/loan";
            Console.WriteLine($"[CommercialBankClient] Full loan URL: {fullLoanUrl}");

            var response = await _client.PostAsJsonAsync(fullLoanUrl, requestBody);
            Console.WriteLine($"[CommercialBankClient] Loan request response status: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[CommercialBankClient] Loan response content: {content}");

            // Check if the response was successful before trying to parse as JSON
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[CommercialBankClient] Loan request failed with status {response.StatusCode}");
                return null;
            }

            // Only try to parse JSON if we have a successful response
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(content);

                // Check if the response indicates success
                if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    if (doc.RootElement.TryGetProperty("loan_number", out var loanNum))
                    {
                        var loanNumber = loanNum.GetString();
                        Console.WriteLine($"[CommercialBankClient] Loan request successful! Loan number: {loanNumber}");
                        return loanNumber;
                    }
                    else
                    {
                        Console.WriteLine($"[CommercialBankClient] Success response but no loan_number found");
                        return null;
                    }
                }
                else
                {
                    // Handle failure response with detailed error information
                    var errorMessage = "Unknown error";
                    var amountRemaining = 0m;

                    if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        errorMessage = errorProp.GetString() ?? "Unknown error";
                    }

                    if (doc.RootElement.TryGetProperty("amount_remaining", out var amountProp))
                    {
                        // Handle amount as either number or string
                        if (amountProp.ValueKind == JsonValueKind.Number)
                        {
                            amountRemaining = amountProp.GetDecimal();
                        }
                        else if (amountProp.ValueKind == JsonValueKind.String)
                        {
                            var amountString = amountProp.GetString();
                            if (!decimal.TryParse(amountString, out amountRemaining))
                            {
                                amountRemaining = 0m;
                            }
                        }
                    }

                    Console.WriteLine($"[CommercialBankClient] Loan request failed - Error: {errorMessage}, Amount remaining: {amountRemaining}");

                    // If the loan was too large, we could potentially retry with the remaining amount
                    if (errorMessage == "loanTooLarge" && amountRemaining > 0)
                    {
                        Console.WriteLine($"[CommercialBankClient] Loan was too large. Remaining amount available: {amountRemaining}");
                    }

                    return null;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[CommercialBankClient] Failed to parse response as JSON: {ex.Message}");
                Console.WriteLine($"[CommercialBankClient] Response content was: {content}");
                return null;
            }
        }

        public async Task<string> MakePaymentAsync(string toAccountNumber, string toBankName, decimal amount, string description)
        {
            var paymentReq = new
            {
                to_account_number = toAccountNumber,
                to_bank_name = toBankName,
                amount,
                description
            };
            var fullTransactionUrl = $"{_client.BaseAddress}/transaction";
            var response = await _client.PostAsJsonAsync(fullTransactionUrl, paymentReq);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (!root.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
            {
                var errorMsg = root.TryGetProperty("error", out var err) ? err.GetString() : "Unknown payment error";
                throw new Exception($"Payment failed: {errorMsg}");
            }
            return root.TryGetProperty("transaction_number", out var txn) ? txn.GetString() ?? string.Empty : string.Empty;
        }

        public async Task<string?> GetAccountDetailsAsync()
        {
            Console.WriteLine("[CommercialBankClient] Getting account details from /api/account/me");
            var fullUrl = $"{_client.BaseAddress}/account/me";
            Console.WriteLine($"[CommercialBankClient] GetAccountDetails using full URL: {fullUrl}");
            var response = await _client.GetAsync(fullUrl);
            Console.WriteLine($"[CommercialBankClient] Account details response status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ CommercialBankClient: Failed to get account details. Status: {response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[CommercialBankClient] Account details response content: {content}");

            using var doc = System.Text.Json.JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Check if the response indicates success (handling new format)
            if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
            {
                if (root.TryGetProperty("account_number", out var accNum))
                {
                    var accountNumber = accNum.GetString();
                    Console.WriteLine($"✅ CommercialBankClient: Account number retrieved: {accountNumber}");
                    return accountNumber;
                }
                else
                {
                    Console.WriteLine("❌ CommercialBankClient: Success response but no account_number found");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("❌ CommercialBankClient: Account details request failed - success field is false or missing");
                return null;
            }
        }

        public async Task<HttpResponseMessage> GetAccountAsync()
        {
            try
            {
                // Use the correct endpoint - construct full URL like CreateAccountAsync does
                var fullUrl = $"{_client.BaseAddress}/account/me";
                Console.WriteLine($"[CommercialBankClient] GetAccount using full URL: {fullUrl}");

                var response = await _client.GetAsync(fullUrl);
                Console.WriteLine($"[CommercialBankClient] GetAccount response status: {response.StatusCode}");
                Console.WriteLine($"[CommercialBankClient] GetAccount response URL: {response.RequestMessage?.RequestUri}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CommercialBankClient] Exception during GET: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
    }
}
