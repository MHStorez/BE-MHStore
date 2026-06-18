namespace MHStore.Services.AdminStatsService;

public class Response
{
    public decimal TodayRevenue { get; set; }
    public int NewOrderCount { get; set; }
    public BestSellingProductResponse? BestSellingProduct { get; set; }
}

public class BestSellingProductResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
