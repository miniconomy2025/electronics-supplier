using esAPI.Exceptions;

namespace esAPI.Clients
{
    public interface ICommercialBankClient
    {
        Task<decimal> GetAccountBalanceAsync();
        Task<string?> CreateAccountAsync();
        Task<string> MakePaymentAsync(string toAccountNumber, string toBankName, decimal amount, string description);
        Task<string?> RequestLoanAsync(decimal amount);

        Task<bool> SetNotificationUrlAsync();
    }

    public class CommercialBankClient(IHttpClientFactory factory) : BaseClient(factory, ClientName), ICommercialBankClient
    {
        private const string ClientName = "commercial-bank";

        public async Task<decimal> GetAccountBalanceAsync()
        {
            var balanceResponse = await GetAsync<BankBalanceResponse>("/account/me/balance");
            return balanceResponse.Balance;

        }

        public async Task<bool> SetNotificationUrlAsync()
        {
            var requestBody = new BankNotificationRequest { NotificationUrl = "https://electronics-supplier-api.projects.bbdgrad.com/payments" };

            try
            {
                var response = await _client.PostAsJsonAsync("/account/me/notify", requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }
                var notifyResponse = await response.Content.ReadFromJsonAsync<BankNotifyResponse>();
                return notifyResponse?.Success ?? false;
            }
            catch (Exception ex) when (ex is not ApiClientException)
            {
                throw;
            }
        }

        public async Task<bool> SetNotificationUrlAsync()
        {
            var requestBody = new { notification_url = "https://electronics-supplier-api.projects.bbdgrad.com/payments" };

            var response = await _client.PostAsJsonAsync("/account/me/notify", requestBody);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("success", out var bal) && bal.GetBoolean();
        }

        public async Task<string?> CreateAccountAsync()
        {
            try
            {
                var response = await _client.PostAsync("/account", null);
                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }
                var accountResponse = await response.Content.ReadFromJsonAsync<BankAccountResponse>();
                if (string.IsNullOrEmpty(accountResponse?.AccountNumber))
                {
                    throw new ApiResponseParseException("Bank API created an account but did not return an account number.");
                }
                return accountResponse.AccountNumber;
            }
            catch (Exception ex) when (ex is not ApiClientException)
            {
                throw;
            }
        }

        public async Task<string?> RequestLoanAsync(decimal amount)
        {
            var requestBody = new BankLoanRequest { Amount = amount };
            var loanResponse = await PostAsync<BankLoanRequest, BankLoanResponse>("/loan", requestBody);

            return loanResponse?.LoanNumber;
        }

        public async Task<string?> RequestLoanAsync(decimal amount)
        {
            var requestBody = new { Amount = amount };

            var response = await _client.PostAsJsonAsync("/loan", requestBody);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("loan_number", out var loanNum) ? loanNum.GetString() : null;;
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
                var response = await _client.PostAsJsonAsync("/transaction", paymentReq);
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
    }
}
