using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderQueryRequest = MHStore.Services.OrderService.OrderQueryRequest;
using OrderRequest = MHStore.Services.OrderService.Request;
using OrderResponse = MHStore.Services.OrderService.Response;
using OrderService = MHStore.Services.OrderService.IService;
using OrderStatusRequest = MHStore.Services.OrderService.StatusRequest;
using DirectBuyRequest = MHStore.Services.OrderService.DirectBuyRequest;

namespace MHStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetRecentOrders([FromQuery] OrderQueryRequest query)
    {
        var orders = await _orderService.GetRecentAsync(query);

        return Ok(orders);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(OrderRequest request)
    {
        try
        {
            var order = await _orderService.CreateAsync(request);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("direct")]
    public async Task<ActionResult<OrderResponse>> CreateDirectOrder(DirectBuyRequest request)
    {
        try
        {
            var order = await _orderService.CreateDirectAsync(request);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> UpdateOrderStatus(Guid id, OrderStatusRequest request)
    {
        return await ExecuteOrderActionAsync(() => _orderService.UpdateStatusAsync(id, request));
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> ConfirmOrder(Guid id)
    {
        return await ExecuteOrderActionAsync(() => _orderService.ConfirmAsync(id));
    }

    [HttpPost("{id}/prepare")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> MarkPreparing(Guid id)
    {
        return await ExecuteOrderActionAsync(() => _orderService.MarkPreparingAsync(id));
    }

    [HttpPost("{id}/deliver")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> MarkDelivering(Guid id)
    {
        return await ExecuteOrderActionAsync(() => _orderService.MarkDeliveringAsync(id));
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> Complete(Guid id)
    {
        return await ExecuteOrderActionAsync(() => _orderService.CompleteAsync(id));
    }

    [HttpPost("{id}/complete-cod")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> CompleteCodAndCollect(Guid id)
    {
        return await ExecuteOrderActionAsync(() => _orderService.CompleteCodAndCollectAsync(id));
    }

    [HttpPost("{id}/confirm-manual-payment")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> ConfirmManualPayment(Guid id)
    {
        return await ExecuteOrderActionAsync(() => _orderService.ConfirmManualTransferPaidAsync(id));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid id)
    {
        return await ExecuteOrderActionAsync(() => _orderService.CancelAsync(id));
    }

    private async Task<ActionResult<OrderResponse>> ExecuteOrderActionAsync(Func<Task<OrderResponse?>> action)
    {
        try
        {
            var order = await action();

            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
