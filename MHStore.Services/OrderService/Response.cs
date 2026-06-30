namespace MHStore.Services.OrderService;

public class Response
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public CustomerInfoResponse CustomerInfo { get; set; } = new();
    public List<OrderItemResponse> Items { get; set; } = [];
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderChannel { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string AddressNote { get; set; } = string.Empty;
    public string AddressReferenceId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CustomerInfoResponse
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Note { get; set; } = string.Empty;
    public string AddressReferenceId { get; set; } = string.Empty;
}

public class OrderItemResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
