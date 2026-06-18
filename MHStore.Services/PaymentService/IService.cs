namespace MHStore.Services.PaymentService;

public interface IService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request);
    Task<SePayWebhookResponse> ProcessSePayWebhookAsync(SePayWebhookRequest request, string apiKey);
}
