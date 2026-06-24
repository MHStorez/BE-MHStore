using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProductRequest = MHStore.Services.ProductService.Request;
using ProductResponse = MHStore.Services.ProductService.Response;
using ProductService = MHStore.Services.ProductService.IService;

namespace MHStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool includeUnavailable = false)
    {
        var canIncludeUnavailable = includeUnavailable && User.IsInRole("Admin");
        var products = await _productService.GetAllAsync(search, category, canIncludeUnavailable);

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetProduct(Guid id)
    {
        var product = await _productService.GetByIdAsync(id, User.IsInRole("Admin"));

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponse>> CreateProduct(ProductRequest request)
    {
        try
        {
            var product = await _productService.CreateAsync(request);

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(Guid id, ProductRequest request)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, request);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var isDeleted = await _productService.DeleteAsync(id);

        return isDeleted ? NoContent() : NotFound();
    }
}
