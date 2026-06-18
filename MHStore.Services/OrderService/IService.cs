namespace MHStore.Services.OrderService;

public interface IService
{
    Task<IEnumerable<Response>> GetRecentAsync(int limit = 50);
    Task<Response?> GetByIdAsync(Guid id);
    Task<Response> CreateAsync(Request request);
    Task<Response?> UpdateStatusAsync(Guid id, StatusRequest request);
}
