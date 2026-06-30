using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CategoryRequest = MHStore.Services.CategoryService.Request;
using CategoryResponse = MHStore.Services.CategoryService.Response;
using CategoryService = MHStore.Services.CategoryService.IService;

namespace MHStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories([FromQuery] bool includeInactive = false)
    {
        var canIncludeInactive = includeInactive && User.IsInRole("Admin");
        var categories = await _categoryService.GetAllAsync(canIncludeInactive);

        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(CategoryRequest request)
    {
        try
        {
            var category = await _categoryService.CreateAsync(request);

            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, category);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(Guid id, CategoryRequest request)
    {
        try
        {
            var category = await _categoryService.UpdateAsync(id, request);

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try
        {
            var isDeleted = await _categoryService.DeleteAsync(id);

            return isDeleted ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
