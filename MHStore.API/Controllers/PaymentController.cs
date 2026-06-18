using Microsoft.AspNetCore.Mvc;
using PaymentRequest = MHStore.Services.PaymentService.CreatePaymentRequest;
using PaymentResponse = MHStore.Services.PaymentService.CreatePaymentResponse;
using PaymentService = MHStore.Services.PaymentService.IService;
using SePayWebhookRequest = MHStore.Services.PaymentService.SePayWebhookRequest;
using SePayWebhookResponse = MHStore.Services.PaymentService.SePayWebhookResponse;

namespace MHStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("create")]
    public async Task<ActionResult<PaymentResponse>> CreatePayment(PaymentRequest request)
    {
        try
        {
            var response = await _paymentService.CreatePaymentAsync(request);

            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, exception.Message);
        }
    }

    [HttpPost("sepay/webhook")]
    public async Task<ActionResult<SePayWebhookResponse>> SePayWebhook(SePayWebhookRequest request)
    {
        var apiKey = GetWebhookApiKey();
        var response = await _paymentService.ProcessSePayWebhookAsync(request, apiKey);

        if (!response.Success && response.Message == "Unauthorized webhook.")
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    private string GetWebhookApiKey()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            var value = authorization.ToString();
            const string bearerPrefix = "Bearer ";

            return value.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
                ? value[bearerPrefix.Length..]
                : value;
        }

        if (Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            return apiKey.ToString();
        }

        return string.Empty;
    }
}
