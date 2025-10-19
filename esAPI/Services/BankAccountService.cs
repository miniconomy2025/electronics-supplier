using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using esAPI.Data;
using esAPI.Clients;

namespace esAPI.Services
{
    public class BankAccountService(AppDbContext db, ICommercialBankClient bankClient, ILogger<BankAccountService> logger, RetryQueuePublisher? retryQueuePublisher)
    {
        private readonly AppDbContext _db = db;
        private readonly ICommercialBankClient _bankClient = bankClient;
        private readonly ILogger<BankAccountService> _logger = logger;
        private readonly RetryQueuePublisher? _retryQueuePublisher = retryQueuePublisher;

        public async Task<(bool Success, string? AccountNumber, string? Error)> SetupBankAccountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🏦 Setting up bank account with commercial bank...");

                var company = await _db.Companies.FirstOrDefaultAsync(c => c.CompanyId == 1, cancellationToken);
                if (company == null)
                {
                    _logger.LogError("❌ Electronics Supplier company (ID=1) not found in database.");
                    return (false, null, "Company not found in database");
                }

                // Check if we already have a bank account
                if (!string.IsNullOrWhiteSpace(company.BankAccountNumber))
                {
                    _logger.LogInformation("🏦 Bank account already exists: {AccountNumber}", company.BankAccountNumber);
                    return (true, company.BankAccountNumber, null);
                }

                _logger.LogInformation("🏦 Creating bank account with notification URL...");

                // Create account with notification URL
                var createAccountRequest = new
                {
                    notification_url = "https://electronics-supplier.tevlen.co.za/payments"
                };

                _logger.LogInformation("🏦 Request body: {@Request}", createAccountRequest);
                _logger.LogInformation("🏦 About to make HTTP request to commercial bank...");

                var createResponse = await _bankClient.CreateAccountAsync(createAccountRequest);

                if (createResponse.IsSuccessStatusCode)
                {
                    var responseContent = await createResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("🏦 Bank account created successfully: {Response}", responseContent);

                    // Parse JSON and extract account number
                    var accountNumber = await ParseAndStoreAccountNumberAsync(responseContent, company, cancellationToken);
                    if (accountNumber != null)
                    {
                        return (true, accountNumber, null);
                    }
                    else
                    {
                        return (false, null, "Failed to parse account number from response");
                    }
                }
                else if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _logger.LogInformation("🏦 Account already exists, retrieving account number...");

                    // Get existing account number
                    var getResponse = await _bankClient.GetAccountAsync();
                    if (getResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await getResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation("🏦 Retrieved existing account number: {Response}", responseContent);

                        // Parse JSON and extract account number
                        var accountNumber = await ParseAndStoreAccountNumberAsync(responseContent, company, cancellationToken);
                        if (accountNumber != null)
                        {
                            return (true, accountNumber, null);
                        }
                        else
                        {
                            return (false, null, "Failed to parse account number from response");
                        }
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to retrieve existing account number. Status: {Status}", getResponse.StatusCode);
                        // Enqueue retry job here
                        if (company != null)
                        {
                            var retryJob = new BankAccountRetryJob
                            {
                                CompanyId = company.CompanyId,
                                NotificationUrl = "https://electronics-supplier.tevlen.co.za/payments", // same as original
                                RetryAttempt = 0
                            };

                            if (_retryQueuePublisher != null)
                            {
                                await _retryQueuePublisher.PublishAsync(retryJob);
                                _logger.LogInformation("🔄 Retry job enqueued for bank account creation.");
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ Retry functionality not available, no retry job enqueued.");
                            }
                        }

                        return (false, null, $"Failed to create bank account. Status: {createResponse.StatusCode}, retry scheduled.");
                    }
                }
                else
                {
                    _logger.LogError("❌ Failed to create bank account. Status: {Status}", createResponse.StatusCode);
                    return (false, null, $"Failed to create bank account. Status: {createResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception during bank account setup");
                return (false, null, ex.Message);
            }
        }

        private async Task<string?> ParseAndStoreAccountNumberAsync(string responseContent, Models.Company company, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("💾 Parsing and storing bank account number: {Response}", responseContent);
                
                // Parse the JSON response to extract the account number
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var actualAccountNumber = responseData.GetProperty("account_number").GetString();
                
                if (string.IsNullOrWhiteSpace(actualAccountNumber))
                {
                    _logger.LogError("❌ No account number found in response");
                    return null;
                }
                
                company.BankAccountNumber = actualAccountNumber;
                await _db.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("✅ Bank account number stored successfully in Company table: {AccountNumber}", actualAccountNumber);
                return actualAccountNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to parse and store account number");
                return null;
            }
        }
    }
}