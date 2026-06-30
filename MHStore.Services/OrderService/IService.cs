namespace MHStore.Services.OrderService;

public interface IService
{
    Task<IEnumerable<Response>> GetRecentAsync(OrderQueryRequest query);
    Task<Response?> GetByIdAsync(Guid id);
    Task<Response> CreateAsync(Request request);
    Task<Response> CreateDirectAsync(DirectBuyRequest request);
    Task<Response?> UpdateStatusAsync(Guid id, StatusRequest request);
    Task<Response?> ConfirmAsync(Guid id);
    Task<Response?> MarkPreparingAsync(Guid id);
    Task<Response?> MarkDeliveringAsync(Guid id);
    Task<Response?> CompleteAsync(Guid id);
    Task<Response?> CompleteCodAndCollectAsync(Guid id);
    Task<Response?> ConfirmManualTransferPaidAsync(Guid id);
    Task<Response?> CancelAsync(Guid id);
}
