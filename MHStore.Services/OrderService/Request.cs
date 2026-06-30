namespace MHStore.Services.OrderService;

public class Request
{
    public CustomerInfoRequest CustomerInfo { get; set; } = new();
    public List<OrderItemRequest> Items { get; set; } = [];
    public string OrderChannel { get; set; } = "Website";
    public string PaymentMethod { get; set; } = "Online";
}

public class DirectBuyRequest
{
    public CustomerInfoRequest CustomerInfo { get; set; } = new();
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string OrderChannel { get; set; } = "Website";
    public string PaymentMethod { get; set; } = "Online";
}

public class OrderQueryRequest
{
    public int Limit { get; set; } = 100;
    public string? OrderChannel { get; set; }
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? Search { get; set; }
}

public class StatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class CustomerInfoRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Note { get; set; } = string.Empty;
    public string AddressReferenceId { get; set; } = string.Empty;
}

public class OrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
