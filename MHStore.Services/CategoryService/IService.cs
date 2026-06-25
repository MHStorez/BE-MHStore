namespace MHStore.Services.CategoryService;

public interface IService
{
    Task<IEnumerable<Response>> GetAllAsync(bool includeInactive = false);
    Task<Response> CreateAsync(Request request);
}
