namespace MHStore.Services.PaymentService;

public class SePayOptions
{
    public string BankCode { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string ContentPrefix { get; set; } = "MHSTORE";
    public string WebhookApiKey { get; set; } = string.Empty;
    public string VietQrTemplate { get; set; } = "compact2";
}
