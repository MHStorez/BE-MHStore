namespace MHStore.Repositories.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerInfo { get; set; } = "{}";
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
    public List<PaymentLog> PaymentLogs { get; set; } = new();
}
