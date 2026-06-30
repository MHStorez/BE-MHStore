using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;
using MHStore.Repositories.Entities;
using MHStore.Repositories.Enums;

namespace MHStore.Services.OrderService;

public class Service : IService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _context;

    public Service(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response>> GetRecentAsync(OrderQueryRequest query)
    {
        query ??= new OrderQueryRequest();
        var safeLimit = Math.Clamp(query.Limit, 1, 200);
        var ordersQuery = _context.Orders.Include(o => o.Items).AsNoTracking();

        if (TryParseEnum<OrderChannel>(query.OrderChannel, out var channel))
        {
            ordersQuery = ordersQuery.Where(order => order.OrderChannel == channel);
        }

        if (TryParseEnum<OrderStatus>(query.OrderStatus, out var orderStatus))
        {
            ordersQuery = ordersQuery.Where(order => order.OrderStatus == orderStatus);
        }

        if (TryParseEnum<PaymentStatus>(query.PaymentStatus, out var paymentStatus))
        {
            ordersQuery = ordersQuery.Where(order => order.PaymentStatus == paymentStatus);
        }

        if (TryParseEnum<PaymentMethod>(query.PaymentMethod, out var paymentMethod))
        {
            ordersQuery = ordersQuery.Where(order => order.PaymentMethod == paymentMethod);
        }

        if (query.CreatedFrom.HasValue)
        {
            ordersQuery = ordersQuery.Where(order => order.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            ordersQuery = ordersQuery.Where(order => order.CreatedAt < query.CreatedTo.Value.AddDays(1));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            ordersQuery = ordersQuery.Where(order =>
                order.OrderCode.ToLower().Contains(search) ||
                order.ReceiverName.ToLower().Contains(search) ||
                order.ReceiverPhone.ToLower().Contains(search));
        }

        var orders = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Take(safeLimit)
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
        var channel = ParseEnum<OrderChannel>(request.OrderChannel, nameof(request.OrderChannel));
        var method = ParseEnum<PaymentMethod>(request.PaymentMethod, nameof(request.PaymentMethod));
        ValidateChannelMethod(channel, method);

        return await CreateOrderAsync(request.CustomerInfo, request.Items, channel, method);
    }

    public async Task<Response> CreateDirectAsync(DirectBuyRequest request)
    {
        Validate(request);
        var channel = ParseEnum<OrderChannel>(request.OrderChannel, nameof(request.OrderChannel));
        var method = ParseEnum<PaymentMethod>(request.PaymentMethod, nameof(request.PaymentMethod));
        ValidateChannelMethod(channel, method);

        return await CreateOrderAsync(
            request.CustomerInfo,
            [new OrderItemRequest
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity
            }],
            channel,
            method);
    }

    public async Task<Response?> UpdateStatusAsync(Guid id, StatusRequest request)
    {
        var status = Validate(request);
        var orderStatus = ParseEnum<OrderStatus>(status, nameof(request.Status));

        return orderStatus switch
        {
            OrderStatus.Confirmed => await ConfirmAsync(id),
            OrderStatus.Preparing => await MarkPreparingAsync(id),
            OrderStatus.Delivering => await MarkDeliveringAsync(id),
            OrderStatus.Completed => await CompleteAsync(id),
            OrderStatus.Cancelled => await CancelAsync(id),
            _ => throw new ArgumentException("Unsupported order status transition.")
        };
    }

    public async Task<Response?> ConfirmAsync(Guid id)
    {
        var order = await FindTrackedOrderAsync(id);
        if (order == null) return null;

        RequireOrderStatus(order, OrderStatus.PendingConfirmation);
        order.OrderStatus = OrderStatus.Confirmed;
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    public async Task<Response?> MarkPreparingAsync(Guid id)
    {
        var order = await FindTrackedOrderAsync(id);
        if (order == null) return null;

        RequireOrderStatus(order, OrderStatus.Confirmed);
        order.OrderStatus = OrderStatus.Preparing;
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    public async Task<Response?> MarkDeliveringAsync(Guid id)
    {
        var order = await FindTrackedOrderAsync(id);
        if (order == null) return null;

        RequireOrderStatus(order, OrderStatus.Preparing);
        order.OrderStatus = OrderStatus.Delivering;
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    public async Task<Response?> CompleteAsync(Guid id)
    {
        var order = await FindTrackedOrderAsync(id);
        if (order == null) return null;

        if (order.OrderStatus != OrderStatus.Delivering)
        {
            throw new ArgumentException("Only delivering orders can be completed.");
        }

        if (order.PaymentMethod == PaymentMethod.COD && order.PaymentStatus != PaymentStatus.Paid)
        {
            throw new ArgumentException("Use COD delivery success action to collect payment and complete the order.");
        }

        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            throw new ArgumentException("Only paid orders can be completed.");
        }

        order.OrderStatus = OrderStatus.Completed;
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    public async Task<Response?> CompleteCodAndCollectAsync(Guid id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var order = await FindTrackedOrderAsync(id);
        if (order == null) return null;

        if (order.PaymentMethod != PaymentMethod.COD)
        {
            throw new ArgumentException("Only COD orders can use this action.");
        }

        RequireOrderStatus(order, OrderStatus.Delivering);
        order.PaymentStatus = PaymentStatus.Paid;
        order.OrderStatus = OrderStatus.Completed;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToResponse(order);
    }

    public async Task<Response?> ConfirmManualTransferPaidAsync(Guid id)
    {
        var order = await FindTrackedOrderAsync(id);
        if (order == null) return null;

        if (order.PaymentMethod != PaymentMethod.ManualTransfer)
        {
            throw new ArgumentException("Only manual transfer orders can be confirmed as paid manually.");
        }

        if (order.OrderStatus is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            throw new ArgumentException("Cannot update payment for cancelled or completed orders.");
        }

        order.PaymentStatus = PaymentStatus.Paid;
        await _context.SaveChangesAsync();

        return ToResponse(order);
    }

    public async Task<Response?> CancelAsync(Guid id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var order = await FindTrackedOrderAsync(id);
        if (order == null) return null;

        if (order.OrderStatus == OrderStatus.Cancelled)
        {
            throw new ArgumentException("Order is already cancelled.");
        }

        if (order.OrderStatus == OrderStatus.Completed)
        {
            throw new ArgumentException("Completed orders cannot be cancelled.");
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            throw new ArgumentException("Paid orders require a refund flow before cancellation.");
        }

        if (!order.StockReleased)
        {
            await RestoreStockAsync(order.Items);
            order.StockReleased = true;
        }

        order.OrderStatus = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToResponse(order);
    }

    private async Task<Order?> FindTrackedOrderAsync(Guid id) =>
        await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

    private async Task RestoreStockAsync(IEnumerable<OrderItem> items)
    {
        var groupedItems = items
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToList();
        var productIds = groupedItems.Select(item => item.ProductId).ToList();
        var products = await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        foreach (var item in groupedItems)
        {
            if (products.TryGetValue(item.ProductId, out var product))
            {
                product.Stock += item.Quantity;
            }
        }
    }

    private async Task<Response> CreateOrderAsync(
        CustomerInfoRequest customerInfo,
        List<OrderItemRequest> requestedItems,
        OrderChannel channel,
        PaymentMethod method)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var parsedItems = ParseRequestedItems(requestedItems);
        var productIds = parsedItems.Select(item => item.ProductId).Distinct().ToList();
        var products = await _context.Products
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

            if (product.Stock < item.Quantity)
            {
                throw new ArgumentException($"Product '{product.Name}' does not have enough stock.");
            }

            product.Stock -= item.Quantity;

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
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            OrderCode = GenerateOrderCode(orderId),
            CustomerInfo = JsonSerializer.Serialize(customer, JsonOptions),
            TotalPrice = orderItems.Sum(item => item.UnitPrice * item.Quantity),
            OrderStatus = OrderStatus.PendingConfirmation,
            PaymentStatus = method == PaymentMethod.Online ? PaymentStatus.Pending : PaymentStatus.Unpaid,
            OrderChannel = channel,
            PaymentMethod = method,
            ReceiverName = customer.Name,
            ReceiverPhone = customer.Phone,
            DeliveryAddress = customer.Address,
            Latitude = customer.Latitude,
            Longitude = customer.Longitude,
            AddressNote = customer.Note,
            AddressReferenceId = customer.AddressReferenceId,
            CreatedAt = DateTime.UtcNow,
            Items = orderItems
        };

        foreach (var item in order.Items)
        {
            item.OrderId = order.Id;
        }

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

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

        if (customerInfo.Latitude is < -90 or > 90)
        {
            throw new ArgumentException("Latitude is invalid.");
        }

        if (customerInfo.Longitude is < -180 or > 180)
        {
            throw new ArgumentException("Longitude is invalid.");
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

    private static void ValidateChannelMethod(OrderChannel channel, PaymentMethod method)
    {
        if (channel == OrderChannel.Website && method != PaymentMethod.Online)
        {
            throw new ArgumentException("Website orders must use online payment.");
        }

        if (channel == OrderChannel.Zalo && method == PaymentMethod.Online)
        {
            throw new ArgumentException("Zalo orders must use COD or manual transfer.");
        }
    }

    private static void RequireOrderStatus(Order order, OrderStatus status)
    {
        if (order.OrderStatus != status)
        {
            throw new ArgumentException($"Order must be {status} to use this action.");
        }
    }

    private static bool TryParseEnum<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        return Enum.TryParse(value.Trim(), true, out result);
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value?.Trim(), true, out var parsed))
        {
            throw new ArgumentException($"{fieldName} is invalid.");
        }

        return parsed;
    }

    private static string GenerateOrderCode(Guid orderId) =>
        $"MH{DateTime.UtcNow:yyMMdd}{orderId:N}"[..16].ToUpperInvariant();

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
            Latitude = customer.Latitude,
            Longitude = customer.Longitude,
            Note = customer.Note.Trim(),
            AddressReferenceId = customer.AddressReferenceId.Trim()
        };
    }

    private static Response ToResponse(Order order)
    {
        var legacyCustomer = DeserializeCustomer(order.CustomerInfo);
        var customer = new CustomerInfoResponse
        {
            Name = string.IsNullOrWhiteSpace(order.ReceiverName) ? legacyCustomer.Name : order.ReceiverName,
            Phone = string.IsNullOrWhiteSpace(order.ReceiverPhone) ? legacyCustomer.Phone : order.ReceiverPhone,
            Address = string.IsNullOrWhiteSpace(order.DeliveryAddress) ? legacyCustomer.Address : order.DeliveryAddress,
            Latitude = order.Latitude ?? legacyCustomer.Latitude,
            Longitude = order.Longitude ?? legacyCustomer.Longitude,
            Note = string.IsNullOrWhiteSpace(order.AddressNote) ? legacyCustomer.Note : order.AddressNote,
            AddressReferenceId = string.IsNullOrWhiteSpace(order.AddressReferenceId) ? legacyCustomer.AddressReferenceId : order.AddressReferenceId
        };
        var orderStatus = order.OrderStatus.ToString();

        return new Response
        {
            Id = order.Id,
            OrderCode = string.IsNullOrWhiteSpace(order.OrderCode) ? order.Id.ToString("N")[..8].ToUpperInvariant() : order.OrderCode,
            CustomerInfo = customer,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId.ToString(),
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            TotalPrice = order.TotalPrice,
            Status = orderStatus,
            OrderStatus = orderStatus,
            PaymentStatus = order.PaymentStatus.ToString(),
            OrderChannel = order.OrderChannel.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            ReceiverName = customer.Name,
            ReceiverPhone = customer.Phone,
            DeliveryAddress = customer.Address,
            Latitude = customer.Latitude,
            Longitude = customer.Longitude,
            AddressNote = customer.Note,
            AddressReferenceId = customer.AddressReferenceId,
            CreatedAt = order.CreatedAt
        };
    }

    private static CustomerInfoResponse DeserializeCustomer(string customerInfo)
    {
        if (string.IsNullOrWhiteSpace(customerInfo))
        {
            return new CustomerInfoResponse();
        }

        try
        {
            return JsonSerializer.Deserialize<CustomerInfoResponse>(customerInfo, JsonOptions) ?? new CustomerInfoResponse();
        }
        catch (JsonException)
        {
            return new CustomerInfoResponse();
        }
    }
}
