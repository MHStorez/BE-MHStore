using MHStore.Repositories.Enums;

namespace MHStore.Repositories.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerInfo { get; set; } = "{}";
    public decimal TotalPrice { get; set; }
    public OrderStatus OrderStatus { get; set; } = OrderStatus.PendingConfirmation;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public OrderChannel OrderChannel { get; set; } = OrderChannel.Website;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Online;
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string AddressNote { get; set; } = string.Empty;
    public string AddressReferenceId { get; set; } = string.Empty;
    public bool StockReleased { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
    public List<PaymentLog> PaymentLogs { get; set; } = new();
}
