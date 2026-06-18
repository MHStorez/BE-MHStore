using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderRequest = MHStore.Services.OrderService.Request;
using OrderResponse = MHStore.Services.OrderService.Response;
using OrderService = MHStore.Services.OrderService.IService;
using OrderStatusRequest = MHStore.Services.OrderService.StatusRequest;

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
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetRecentOrders([FromQuery] int limit = 50)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var orders = await _orderService.GetRecentAsync(safeLimit);

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

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderResponse>> UpdateOrderStatus(Guid id, OrderStatusRequest request)
    {
        try
        {
            var order = await _orderService.UpdateStatusAsync(id, request);

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
