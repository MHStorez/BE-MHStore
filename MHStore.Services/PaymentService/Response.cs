namespace MHStore.Services.PaymentService;

public class CreatePaymentResponse
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
}

public class SePayWebhookResponse
{
    public Guid? OrderId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
