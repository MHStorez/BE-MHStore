using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;
using MHStore.Repositories.Enums;
using MHStore.Services.OrderService;

namespace MHStore.Services.AdminStatsService;

public class Service : IService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
                order.OrderStatus == OrderStatus.Completed &&
                order.CreatedAt >= today &&
                order.CreatedAt < tomorrow)
            .SumAsync(order => order.TotalPrice);
        var newOrderCount = await _context.Orders
            .AsNoTracking()
            .CountAsync(order =>
                order.OrderStatus == OrderStatus.PendingConfirmation &&
                order.CreatedAt >= today &&
                order.CreatedAt < tomorrow);
        var pendingOrderCount = await _context.Orders
            .AsNoTracking()
            .CountAsync(order => order.OrderStatus == OrderStatus.PendingConfirmation);
        var completedOrderCount = await _context.Orders
            .AsNoTracking()
            .CountAsync(order => order.OrderStatus == OrderStatus.Completed);
        var totalOrderCount = await _context.Orders
            .AsNoTracking()
            .CountAsync();
        var totalRevenue = await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderStatus == OrderStatus.Completed)
            .SumAsync(order => order.TotalPrice);
        var topProducts = await _context.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.OrderStatus == OrderStatus.Completed)
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
            .Take(5)
            .ToListAsync();
        var completedCustomerOrders = await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderStatus == OrderStatus.Completed)
            .Select(order => new
            {
                order.CustomerInfo,
                order.ReceiverName,
                order.ReceiverPhone,
                order.TotalPrice,
                order.CreatedAt
            })
            .ToListAsync();
        var topCustomers = completedCustomerOrders
            .Select(order => new
            {
                Customer = DeserializeCustomer(order.CustomerInfo),
                order.ReceiverName,
                order.ReceiverPhone,
                order.TotalPrice,
                order.CreatedAt
            })
            .Select(order => new
            {
                Customer = new CustomerInfoResponse
                {
                    Name = string.IsNullOrWhiteSpace(order.ReceiverName) ? order.Customer?.Name ?? string.Empty : order.ReceiverName,
                    Phone = string.IsNullOrWhiteSpace(order.ReceiverPhone) ? order.Customer?.Phone ?? string.Empty : order.ReceiverPhone
                },
                order.TotalPrice,
                order.CreatedAt
            })
            .GroupBy(order => NormalizePhone(order.Customer.Phone))
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(group =>
            {
                var latestOrder = group.OrderByDescending(order => order.CreatedAt).First();

                return new CustomerSummaryResponse
                {
                    Name = string.IsNullOrWhiteSpace(latestOrder.Customer.Name)
                        ? "Khách chưa nhập tên"
                        : latestOrder.Customer.Name.Trim(),
                    Phone = latestOrder.Customer.Phone.Trim(),
                    OrderCount = group.Count(),
                    TotalSpent = group.Sum(order => order.TotalPrice),
                    LastOrderAt = group.Max(order => order.CreatedAt)
                };
            })
            .OrderByDescending(customer => customer.TotalSpent)
            .ThenByDescending(customer => customer.OrderCount)
            .Take(5)
            .ToList();

        return new Response
        {
            TodayRevenue = todayRevenue,
            NewOrderCount = newOrderCount,
            PendingOrderCount = pendingOrderCount,
            CompletedOrderCount = completedOrderCount,
            TotalOrderCount = totalOrderCount,
            TotalRevenue = totalRevenue,
            BestSellingProduct = topProducts.FirstOrDefault(),
            TopProducts = topProducts,
            TopCustomers = topCustomers
        };
    }

    private static CustomerInfoResponse? DeserializeCustomer(string customerInfo)
    {
        if (string.IsNullOrWhiteSpace(customerInfo))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CustomerInfoResponse>(customerInfo, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizePhone(string phone) =>
        new(phone.Where(char.IsDigit).ToArray());
}
