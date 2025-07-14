using esAPI.DTOs;
using esAPI.Interfaces;

namespace esAPI.Clients;

public class RecyclerApiClient : BaseClient, ISupplierApiClient
{
    private const string ClientName = "recycler";
    private const string OurCompanyName = "electronics-supplier";

    public RecyclerApiClient(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, ClientName) { }

    public async Task<List<SupplierMaterialInfo>> GetAvailableMaterialsAsync()
    {
        // The API returns a raw array, not an object
        var materials = await GetAsync<List<RecyclerMaterialDto>>("/materials");
        if (materials == null)
        {
            return new List<SupplierMaterialInfo>();
        }

        // Only keep Copper and Silicon
        var filtered = materials
            .Where(m => m.Name.Equals("Copper", StringComparison.OrdinalIgnoreCase) || m.Name.Equals("Silicon", StringComparison.OrdinalIgnoreCase))
            .Select(m => new SupplierMaterialInfo
            {
                MaterialName = m.Name,
                AvailableQuantity = m.AvailableQuantityInKg,
                PricePerKg = m.Price
            })
            .ToList();

        return filtered;
    }

    public async Task<SupplierOrderResponse?> PlaceOrderAsync(SupplierOrderRequest request)
    {

        var recyclerRequest = new RecyclerOrderRequestDto
        {
            CompanyName = OurCompanyName,
            OrderItems = new List<RecyclerOrderItemDto>
            {
                new RecyclerOrderItemDto
                {
                    RawMaterialName = request.MaterialName,
                    QuantityInKg = request.WeightQuantity
                }
            }
        };

        return await PostAsync<RecyclerOrderRequestDto, SupplierOrderResponse>("/orders", recyclerRequest);
    }

    public async Task<RecyclerOrderResponseWrapper?> PlaceRecyclerOrderAsync(string materialName, int quantityInKg)
    {
        var recyclerRequest = new RecyclerOrderRequestDto
        {
            CompanyName = OurCompanyName,
            OrderItems = new List<RecyclerOrderItemDto>
            {
                new RecyclerOrderItemDto
                {
                    RawMaterialName = materialName,
                    QuantityInKg = quantityInKg
                }
            }
        };
        return await PostAsync<RecyclerOrderRequestDto, RecyclerOrderResponseWrapper>("/orders", recyclerRequest);
    }

    public async Task<List<string>> OrderAndPayForHalfStockAsync(ICommercialBankClient bankClient, ILogger logger)
    {
        var actions = new List<string>();
        try
        {
            var materials = await GetAvailableMaterialsAsync();
            foreach (var mat in materials)
            {
                if (mat.AvailableQuantity > 0)
                {
                    int orderQty = mat.AvailableQuantity / 2;
                    if (orderQty == 0) continue;
                    logger.LogInformation($"🛒 Placing recycler order for {orderQty} kg of {mat.MaterialName}");
                    var orderResponse = await PlaceRecyclerOrderAsync(mat.MaterialName, orderQty);
                    if (orderResponse?.IsSuccess == true && orderResponse.Data != null)
                    {
                        var orderId = orderResponse.Data.OrderId;
                        var total = orderResponse.Data.Total;
                        var accountNumber = orderResponse.Data.AccountNumber;
                        logger.LogInformation($"✅ Recycler order placed: OrderId={orderId}, Total={total}, Account={accountNumber}");
                        actions.Add($"Ordered {orderQty}kg {mat.MaterialName} (OrderId={orderId}, Total={total})");
                        if (!string.IsNullOrEmpty(accountNumber) && total > 0)
                        {
                            logger.LogInformation($"💸 Paying recycler {total} for order {orderId}");
                            await bankClient.MakePaymentAsync(accountNumber, "commercial-bank", total, orderId.ToString());
                            logger.LogInformation($"✅ Payment sent for recycler order {orderId}");
                            actions.Add($"Paid {total} to {accountNumber} for order {orderId}");
                        }
                    }
                    else
                    {
                        logger.LogWarning($"❌ Failed to place recycler order for {mat.MaterialName}");
                        actions.Add($"Failed to order {mat.MaterialName}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error during recycler order/payment logic");
            actions.Add($"Exception: {ex.Message}");
        }
        return actions;
    }

    public class RecyclerOrderPaymentInfo
    {
        public string MaterialName { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public decimal Total { get; set; }
        public string? AccountNumber { get; set; }
    }

    public async Task<List<RecyclerOrderPaymentInfo>> PlaceHalfStockOrdersAsync()
    {
        var results = new List<RecyclerOrderPaymentInfo>();
        var materials = await GetAvailableMaterialsAsync();
        foreach (var mat in materials)
        {
            if (mat.AvailableQuantity > 0)
            {
                int orderQty = mat.AvailableQuantity / 2;
                if (orderQty == 0) continue;
                var orderResponse = await PlaceRecyclerOrderAsync(mat.MaterialName, orderQty);
                if (orderResponse?.IsSuccess == true && orderResponse.Data != null)
                {
                    results.Add(new RecyclerOrderPaymentInfo
                    {
                        MaterialName = mat.MaterialName,
                        OrderId = orderResponse.Data.OrderId,
                        Total = orderResponse.Data.Total,
                        AccountNumber = orderResponse.Data.AccountNumber
                    });
                }
            }
        }
        return results;
    }
}