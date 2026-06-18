using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MHStore.Repositories.Data;

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

        var content = BuildTransferContent(order.Id);

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
            return new SePayWebhookResponse
            {
                Success = false,
                Message = "Unauthorized webhook."
            };
        }

        if (!string.Equals(request.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            return new SePayWebhookResponse
            {
                Success = true,
                Message = "Ignored non-incoming transaction."
            };
        }

        var orderId = ExtractOrderId(request.Code, request.Content, request.Description);

        if (orderId == null)
        {
            return new SePayWebhookResponse
            {
                Success = false,
                Message = "Order reference was not found."
            };
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value);

        if (order == null)
        {
            return new SePayWebhookResponse
            {
                OrderId = orderId,
                Success = false,
                Message = "Order was not found."
            };
        }

        if (request.TransferAmount < order.TotalPrice)
        {
            order.Status = "PaymentFailed";
            await _context.SaveChangesAsync();

            return new SePayWebhookResponse
            {
                OrderId = order.Id,
                Success = false,
                Message = "Transfer amount is lower than order total."
            };
        }

        order.Status = "Completed";
        await _context.SaveChangesAsync();

        return new SePayWebhookResponse
        {
            OrderId = order.Id,
            Success = true,
            Message = "Payment completed."
        };
    }

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

    private string BuildTransferContent(Guid orderId)
    {
        return $"{_options.ContentPrefix}{orderId:N}";
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
