using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;
using MHStore.Repositories.Entities;

namespace MHStore.Services.OrderService;

public class Service : IService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedStatuses = ["Pending", "Completed", "PaymentFailed"];
    private readonly AppDbContext _context;

    public Service(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response>> GetRecentAsync(int limit = 50)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return orders.Select(ToResponse);
    }

    public async Task<Response?> GetByIdAsync(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? null : ToResponse(order);
    }

    public async Task<Response> CreateAsync(Request request)
    {
        Validate(request);

        var customer = Sanitize(request.CustomerInfo);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerInfo = JsonSerializer.Serialize(customer, JsonOptions),
            TotalPrice = request.Items.Sum(item => item.UnitPrice * item.Quantity),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        order.Items = request.Items.Select(item => new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = Guid.Parse(item.ProductId),
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }).ToList();

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    public async Task<Response?> UpdateStatusAsync(Guid id, StatusRequest request)
    {
        var status = request.Status.Trim();

        if (!AllowedStatuses.Contains(status))
        {
            throw new ArgumentException("Order status must be Pending, Completed, or PaymentFailed.");
        }

        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        order.Status = status;
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    private static void Validate(Request request)
    {
        if (request.Items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.");
        }

        if (request.Items.Any(item => string.IsNullOrWhiteSpace(item.ProductName)))
        {
            throw new ArgumentException("Every order item must have a product name.");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            throw new ArgumentException("Every order item quantity must be greater than zero.");
        }

        if (request.Items.Any(item => item.UnitPrice <= 0))
        {
            throw new ArgumentException("Every order item unit price must be greater than zero.");
        }
    }

    private static CustomerInfoResponse Sanitize(CustomerInfoRequest customer)
    {
        return new CustomerInfoResponse
        {
            Name = customer.Name.Trim(),
            Phone = customer.Phone.Trim(),
            Address = customer.Address.Trim(),
            Note = customer.Note.Trim()
        };
    }

    private static Response ToResponse(Order order)
    {
        return new Response
        {
            Id = order.Id,
            CustomerInfo = JsonSerializer.Deserialize<CustomerInfoResponse>(order.CustomerInfo, JsonOptions) ?? new CustomerInfoResponse(),
            Items = order.Items.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId.ToString(),
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            CreatedAt = order.CreatedAt
        };
    }
}
