using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MHStore.Repositories.Data;
using MHStore.Repositories.Entities;
using MHStore.Repositories.Enums;

namespace MHStore.Services.PaymentService;

public class Service : IService
{
    private readonly AppDbContext _context;
    private readonly SePayOptions _options;

    public Service(AppDbContext context, IOptions<SePayOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        Validate(request);
        EnsureConfigured();

        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order == null)
        {
            throw new ArgumentException("Order was not found.");
        }

        if (order.TotalPrice <= 0)
        {
            throw new ArgumentException("Order total must be greater than zero.");
        }

        if (order.OrderChannel != OrderChannel.Website || order.PaymentMethod != PaymentMethod.Online)
        {
            throw new ArgumentException("Only website online orders can create a SePay payment request.");
        }

        if (order.PaymentStatus != PaymentStatus.Pending)
        {
            throw new ArgumentException("Only pending online payments can create a new payment request.");
        }

        if (order.OrderStatus is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            throw new ArgumentException("Cancelled or completed orders cannot create a new payment request.");
        }

        var content = BuildTransferContent(order);

        return new CreatePaymentResponse
        {
            OrderId = order.Id,
            Amount = order.TotalPrice,
            BankCode = _options.BankCode,
            AccountNumber = _options.AccountNumber,
            AccountName = _options.AccountName,
            TransferContent = content,
            QrImageUrl = BuildQrImageUrl(order.TotalPrice, content)
        };
    }

    public async Task<SePayWebhookResponse> ProcessSePayWebhookAsync(SePayWebhookRequest request, string apiKey)
    {
        if (!IsAuthorized(apiKey))
        {
            return new SePayWebhookResponse { Success = false, Message = "Unauthorized webhook." };
        }

        if (request == null)
        {
            return new SePayWebhookResponse { Success = false, Message = "Webhook payload is required." };
        }

        if (!string.Equals(request.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            return new SePayWebhookResponse { Success = true, Message = "Ignored non-incoming transaction." };
        }

        if (request.TransferAmount <= 0)
        {
            return new SePayWebhookResponse { Success = false, Message = "Transfer amount must be greater than zero." };
        }

        var transactionId = GetTransactionId(request);
        var duplicateLog = await _context.PaymentLogs.AsNoTracking().FirstOrDefaultAsync(log => log.TransactionId == transactionId);

        if (duplicateLog != null)
        {
            return new SePayWebhookResponse
            {
                OrderId = duplicateLog.OrderId,
                Success = true,
                Message = "Duplicate webhook ignored."
            };
        }

        var order = await FindOrderFromWebhookAsync(request);

        if (order == null)
        {
            return new SePayWebhookResponse
            {
                Success = false,
                Message = "Order reference was not found."
            };
        }

        if (order.OrderStatus is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            await AddPaymentLogAsync(order, request, "Ignored");
            await _context.SaveChangesAsync();

            return new SePayWebhookResponse
            {
                OrderId = order.Id,
                Success = true,
                Message = "Order cannot be updated by webhook in its current status."
            };
        }

        if (order.OrderChannel != OrderChannel.Website || order.PaymentMethod != PaymentMethod.Online)
        {
            await AddPaymentLogAsync(order, request, "Ignored");
            await _context.SaveChangesAsync();

            return new SePayWebhookResponse
            {
                OrderId = order.Id,
                Success = true,
                Message = "Ignored payment for non-online order."
            };
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            await AddPaymentLogAsync(order, request, "Ignored");
            await _context.SaveChangesAsync();

            return new SePayWebhookResponse
            {
                OrderId = order.Id,
                Success = true,
                Message = "Order was already paid."
            };
        }

        if (order.PaymentStatus != PaymentStatus.Pending ||
            order.OrderStatus is not (OrderStatus.PendingConfirmation or OrderStatus.Confirmed))
        {
            await AddPaymentLogAsync(order, request, "Ignored");
            await _context.SaveChangesAsync();

            return new SePayWebhookResponse
            {
                OrderId = order.Id,
                Success = false,
                Message = "Order is not in a valid state for payment confirmation."
            };
        }

        if (request.TransferAmount < order.TotalPrice)
        {
            order.PaymentStatus = PaymentStatus.Failed;
            await AddPaymentLogAsync(order, request, "Failed");
            await _context.SaveChangesAsync();

            return new SePayWebhookResponse
            {
                OrderId = order.Id,
                Success = false,
                Message = "Transfer amount is lower than order total."
            };
        }

        order.PaymentStatus = PaymentStatus.Paid;
        order.OrderStatus = OrderStatus.Confirmed;
        await AddPaymentLogAsync(order, request, "Paid");
        await _context.SaveChangesAsync();

        return new SePayWebhookResponse
        {
            OrderId = order.Id,
            Success = true,
            Message = "Payment completed. Order is confirmed."
        };
    }

    private static void Validate(CreatePaymentRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Payment request is required.");
        }

        if (request.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.");
        }
    }

    private async Task<Order?> FindOrderFromWebhookAsync(SePayWebhookRequest request)
    {
        var orderCode = ExtractOrderCode(request.Code, request.Content, request.Description);

        if (!string.IsNullOrWhiteSpace(orderCode))
        {
            var byCode = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (byCode != null) return byCode;
        }

        var orderId = ExtractOrderId(request.Code, request.Content, request.Description);

        return orderId == null ? null : await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value);
    }

    private async Task AddPaymentLogAsync(Order order, SePayWebhookRequest request, string status)
    {
        var transactionId = GetTransactionId(request);

        if (await _context.PaymentLogs.AnyAsync(log => log.TransactionId == transactionId))
        {
            return;
        }

        await _context.PaymentLogs.AddAsync(new PaymentLog
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            TransactionId = transactionId,
            Amount = request.TransferAmount,
            Status = status,
            RawData = JsonSerializer.Serialize(request),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string GetTransactionId(SePayWebhookRequest request) =>
        string.IsNullOrWhiteSpace(request.ReferenceCode)
            ? request.Id.ToString(CultureInfo.InvariantCulture)
            : request.ReferenceCode.Trim();

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BankCode) ||
            string.IsNullOrWhiteSpace(_options.AccountNumber) ||
            string.IsNullOrWhiteSpace(_options.AccountName))
        {
            throw new InvalidOperationException("SePay settings are not configured.");
        }
    }

    private bool IsAuthorized(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookApiKey))
        {
            return true;
        }

        return string.Equals(apiKey, _options.WebhookApiKey, StringComparison.Ordinal);
    }

    private string BuildQrImageUrl(decimal amount, string content)
    {
        var amountValue = decimal.ToInt64(amount).ToString(CultureInfo.InvariantCulture);
        var encodedContent = Uri.EscapeDataString(content);

        return $"https://qr.sepay.vn/img?acc={_options.AccountNumber}&bank={_options.BankCode}&amount={amountValue}&des={encodedContent}&template={_options.VietQrTemplate}";
    }

    private string BuildTransferContent(Order order)
    {
        var reference = string.IsNullOrWhiteSpace(order.OrderCode) ? order.Id.ToString("N") : order.OrderCode;

        return $"{_options.ContentPrefix}{reference}";
    }

    private string? ExtractOrderCode(params string[] values)
    {
        var prefix = _options.ContentPrefix.Trim();

        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var index = value.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                continue;
            }

            var start = index + prefix.Length;
            var chars = value[start..]
                .TakeWhile(char.IsLetterOrDigit)
                .ToArray();
            var token = new string(chars).Trim();

            if (token.Length is >= 8 and <= 32 && !Guid.TryParseExact(token, "N", out _))
            {
                return token.ToUpperInvariant();
            }
        }

        return null;
    }

    private Guid? ExtractOrderId(params string[] values)
    {
        var prefix = _options.ContentPrefix.Trim();

        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var index = value.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                continue;
            }

            var start = index + prefix.Length;
            var available = value.Length - start;

            if (available < 32)
            {
                continue;
            }

            var token = value.Substring(start, 32);

            if (Guid.TryParseExact(token, "N", out var orderId))
            {
                return orderId;
            }
        }

        return null;
    }
}
