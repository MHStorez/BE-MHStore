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
    private static readonly HashSet<string> AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private readonly ProductService _productService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(ProductService productService, IWebHostEnvironment environment)
    {
        _productService = productService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? category = null,
        [FromQuery] bool includeUnavailable = false)
    {
        var canIncludeUnavailable = includeUnavailable && User.IsInRole("Admin");
        var products = await _productService.GetAllAsync(search, categoryId, category, canIncludeUnavailable);

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

    [HttpPost("image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<ActionResult<object>> UploadProductImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Image file is required.");
        }

        if (file.Length > MaxImageBytes)
        {
            return BadRequest("Image file must be 5MB or smaller.");
        }

        if (!AllowedImageContentTypes.Contains(file.ContentType))
        {
            return BadRequest("Only JPG, PNG, WEBP, and GIF images are allowed.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Image file extension is not supported.");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadDirectory = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(uploadDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadDirectory, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var request = HttpContext.Request;
        var imageUrl = $"{request.Scheme}://{request.Host}/uploads/products/{fileName}";

        return Ok(new { imageUrl });
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
