using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminStatsResponse = MHStore.Services.AdminStatsService.Response;
using AdminStatsService = MHStore.Services.AdminStatsService.IService;

namespace MHStore.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AdminStatsService _adminStatsService;

    public AdminController(AdminStatsService adminStatsService)
    {
        _adminStatsService = adminStatsService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsResponse>> GetStats()
    {
        return Ok(await _adminStatsService.GetTodayStatsAsync());
    }
}
