namespace MHStore.Services.ProductService;

public interface IService
{
    Task<IEnumerable<Response>> GetAllAsync(string? search = null, string? category = null, bool includeUnavailable = false);
    Task<Response?> GetByIdAsync(Guid id);
    Task<Response> CreateAsync(Request request);
    Task<Response?> UpdateAsync(Guid id, Request request);
    Task<bool> DeleteAsync(Guid id);
}
