namespace MHStore.Services.AdminStatsService;

public interface IService
{
    Task<Response> GetTodayStatsAsync();
}
