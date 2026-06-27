using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;
using MHStore.Repositories.Entities;

namespace MHStore.Services.OrderService;

public class Service : IService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedStatuses = ["Completed"];
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

        return await CreateOrderAsync(request.CustomerInfo, request.Items);
    }

    public async Task<Response> CreateDirectAsync(DirectBuyRequest request)
    {
        Validate(request);

        return await CreateOrderAsync(
            request.CustomerInfo,
            [new OrderItemRequest
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity
            }]);
    }

    public async Task<Response?> UpdateStatusAsync(Guid id, StatusRequest request)
    {
        var status = Validate(request);

        if (!AllowedStatuses.Contains(status))
        {
            throw new ArgumentException("Only Completed status can be set by admin.");
        }

        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        if (order.PaymentStatus != "Paid")
        {
            throw new ArgumentException("Only paid orders can be completed.");
        }

        if (order.Status != "Processing")
        {
            throw new ArgumentException("Only processing orders can be completed.");
        }

        order.Status = "Completed";
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    private async Task<Response> CreateOrderAsync(CustomerInfoRequest customerInfo, List<OrderItemRequest> requestedItems)
    {
        var parsedItems = ParseRequestedItems(requestedItems);
        var productIds = parsedItems.Select(item => item.ProductId).Distinct().ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        var orderItems = new List<OrderItem>();

        foreach (var item in parsedItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                throw new ArgumentException("One or more products were not found.");
            }

            if (!product.IsAvailable)
            {
                throw new ArgumentException($"Product '{product.Name}' is not available.");
            }

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        var customer = Sanitize(customerInfo);
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerInfo = JsonSerializer.Serialize(customer, JsonOptions),
            TotalPrice = orderItems.Sum(item => item.UnitPrice * item.Quantity),
            Status = "Pending",
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            Items = orderItems
        };

        foreach (var item in order.Items)
        {
            item.OrderId = order.Id;
        }

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    private static List<ParsedOrderItem> ParseRequestedItems(IEnumerable<OrderItemRequest> items)
    {
        var parsedItems = new List<ParsedOrderItem>();

        foreach (var item in items)
        {
            if (!Guid.TryParse(item.ProductId, out var productId))
            {
                throw new ArgumentException("Every order item must have a valid product id.");
            }

            var existingItem = parsedItems.FirstOrDefault(parsed => parsed.ProductId == productId);

            if (existingItem == null)
            {
                parsedItems.Add(new ParsedOrderItem(productId, item.Quantity));
            }
            else
            {
                existingItem.Quantity += item.Quantity;
            }
        }

        return parsedItems;
    }

    private static void Validate(Request request)
    {
        if (request == null)
        {
            throw new ArgumentException("Order request is required.");
        }

        ValidateCustomerInfo(request.CustomerInfo);
        ValidateOrderItems(request.Items);
    }

    private static void Validate(DirectBuyRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Order request is required.");
        }

        ValidateCustomerInfo(request.CustomerInfo);

        if (!Guid.TryParse(request.ProductId, out _))
        {
            throw new ArgumentException("Every order item must have a valid product id.");
        }

        if (request.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }
    }

    private static string Validate(StatusRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Status))
        {
            throw new ArgumentException("Order status is required.");
        }

        return request.Status.Trim();
    }

    private static void ValidateCustomerInfo(CustomerInfoRequest customerInfo)
    {
        if (customerInfo == null)
        {
            throw new ArgumentException("Customer information is required.");
        }

        if (string.IsNullOrWhiteSpace(customerInfo.Name))
        {
            throw new ArgumentException("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(customerInfo.Phone))
        {
            throw new ArgumentException("Customer phone is required.");
        }

        if (string.IsNullOrWhiteSpace(customerInfo.Address))
        {
            throw new ArgumentException("Customer address is required.");
        }
    }

    private static void ValidateOrderItems(List<OrderItemRequest> items)
    {
        if (items == null || items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.");
        }

        if (items.Any(item => item == null || !Guid.TryParse(item.ProductId, out _)))
        {
            throw new ArgumentException("Every order item must have a valid product id.");
        }

        if (items.Any(item => item.Quantity <= 0))
        {
            throw new ArgumentException("Every order item quantity must be greater than zero.");
        }
    }

    private sealed class ParsedOrderItem
    {
        public ParsedOrderItem(Guid productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }

        public Guid ProductId { get; }
        public int Quantity { get; set; }
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
            PaymentStatus = order.PaymentStatus,
            CreatedAt = order.CreatedAt
        };
    }
}
