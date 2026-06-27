using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;
using MHStore.Repositories.Entities;

namespace MHStore.Services.ProductService;

public class Service : IService
{
    private const string DefaultCategoryName = "Khác";
    private readonly AppDbContext _context;

    public Service(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response>> GetAllAsync(string? search = null, Guid? categoryId = null, string? category = null, bool includeUnavailable = false)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsNoTracking();

        if (!includeUnavailable)
        {
            query = query.Where(p => p.IsAvailable && p.Category.Status == "Active");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(keyword) ||
                (p.Description != null && p.Description.ToLower().Contains(keyword)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(category) && category.Trim() != "Tat ca")
        {
            var selectedCategory = category.Trim().ToLower();
            query = query.Where(p => p.Category.Name.ToLower() == selectedCategory);
        }

        var products = await query
            .OrderBy(p => p.Category.Name)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return products.Select(ToResponse);
    }

    public async Task<Response?> GetByIdAsync(Guid id, bool includeUnavailable = false)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsNoTracking();

        if (!includeUnavailable)
        {
            query = query.Where(p => p.IsAvailable && p.Category.Status == "Active");
        }

        var product = await query.FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : ToResponse(product);
    }

    public async Task<Response> CreateAsync(Request request)
    {
        Validate(request);
        var category = await ResolveCategoryAsync(request);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            ImageUrl = request.ImageUrl?.Trim(),
            CategoryId = category.Id,
            Category = category,
            IsAvailable = request.IsAvailable
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return ToResponse(product);
    }

    public async Task<Response?> UpdateAsync(Guid id, Request request)
    {
        Validate(request);

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return null;
        }

        var category = await ResolveCategoryAsync(request);

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl?.Trim();
        product.CategoryId = category.Id;
        product.Category = category;
        product.IsAvailable = request.IsAvailable;

        await _context.SaveChangesAsync();

        return ToResponse(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return false;
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<Category> ResolveCategoryAsync(Request request)
    {
        if (!string.IsNullOrWhiteSpace(request.NewCategoryName))
        {
            return await GetOrCreateCategoryAsync(request.NewCategoryName.Trim());
        }

        if (request.CategoryId.HasValue)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value);

            if (category == null)
            {
                throw new ArgumentException("Category was not found.");
            }

            return category;
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            return await GetOrCreateCategoryAsync(request.Category.Trim());
        }

        return await GetOrCreateCategoryAsync(DefaultCategoryName);
    }

    private async Task<Category> GetOrCreateCategoryAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.");
        }

        var normalizedName = name.Trim().ToLower();
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == normalizedName);

        if (category != null)
        {
            return category;
        }

        category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = ToSlug(name),
            Status = "Active"
        };

        await _context.Categories.AddAsync(category);
        return category;
    }

    private static void Validate(Request request)
    {
        if (request == null)
        {
            throw new ArgumentException("Product request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (request.Price <= 0)
        {
            throw new ArgumentException("Product price must be greater than zero.");
        }

        if (request.CategoryId == null && string.IsNullOrWhiteSpace(request.Category) && string.IsNullOrWhiteSpace(request.NewCategoryName))
        {
            throw new ArgumentException("Product category is required.");
        }
    }

    private static Response ToResponse(Product product)
    {
        return new Response
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            Category = product.Category?.Name ?? DefaultCategoryName,
            IsAvailable = product.IsAvailable
        };
    }

    private static string ToSlug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        var slug = new string(chars);

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
