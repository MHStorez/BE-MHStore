namespace MHStore.Services.OrderService;

public class Request
{
    public CustomerInfoRequest CustomerInfo { get; set; } = new();
    public List<OrderItemRequest> Items { get; set; } = [];
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
    public string Note { get; set; } = string.Empty;
}

public class OrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
