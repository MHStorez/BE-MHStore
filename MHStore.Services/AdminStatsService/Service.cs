using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;

namespace MHStore.Services.AdminStatsService;

public class Service : IService
{
    private readonly AppDbContext _context;

    public Service(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Response> GetTodayStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayRevenue = await _context.Orders
            .AsNoTracking()
            .Where(order =>
                order.Status == "Completed" &&
                order.CreatedAt >= today &&
                order.CreatedAt < tomorrow)
            .SumAsync(order => order.TotalPrice);
        var newOrderCount = await _context.Orders
            .AsNoTracking()
            .CountAsync(order =>
                order.Status == "Pending" &&
                order.CreatedAt >= today &&
                order.CreatedAt < tomorrow);
        var bestSellingProduct = await _context.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.Order.Status == "Completed" &&
                item.Order.CreatedAt >= today &&
                item.Order.CreatedAt < tomorrow)
            .GroupBy(item => new { item.ProductId, item.ProductName })
            .Select(group => new BestSellingProductResponse
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductName,
                QuantitySold = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.UnitPrice * item.Quantity)
            })
            .OrderByDescending(item => item.QuantitySold)
            .ThenByDescending(item => item.Revenue)
            .FirstOrDefaultAsync();

        return new Response
        {
            TodayRevenue = todayRevenue,
            NewOrderCount = newOrderCount,
            BestSellingProduct = bestSellingProduct
        };
    }
}
