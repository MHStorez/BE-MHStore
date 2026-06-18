namespace MHStore.Services.AdminStatsService;

public class Response
{
    public decimal TodayRevenue { get; set; }
    public int NewOrderCount { get; set; }
    public int PendingOrderCount { get; set; }
    public int CompletedOrderCount { get; set; }
    public int TotalOrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public BestSellingProductResponse? BestSellingProduct { get; set; }
    public List<BestSellingProductResponse> TopProducts { get; set; } = [];
    public List<CustomerSummaryResponse> TopCustomers { get; set; } = [];
}

public class BestSellingProductResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class CustomerSummaryResponse
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime LastOrderAt { get; set; }
}
