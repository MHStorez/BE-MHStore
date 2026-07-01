using Microsoft.AspNetCore.Mvc;
using AccountModels = MHStore.Services.AccountService;
using AccountService = MHStore.Services.AccountService.IService;

namespace MHStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AccountModels.AuthResponse>> Register(AccountModels.RegisterRequest request)
    {
        try
        {
            return Ok(await _accountService.RegisterAsync(request));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccountModels.AuthResponse>> Login(AccountModels.LoginRequest request)
    {
        try
        {
            return Ok(await _accountService.LoginAsync(request));
        }
        catch (ArgumentException exception)
        {
            return Unauthorized(exception.Message);
        }
    }
        [HttpPost("test")]
    public async Task<ActionResult<AccountModels.AuthResponse>> Test(AccountModels.LoginRequest request)
    {
        try
        {
            return Ok(await _accountService.LoginAsync(request));
        }
        catch (ArgumentException exception)
        {
            return Unauthorized(exception.Message);
        }
    }
}
