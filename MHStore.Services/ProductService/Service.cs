using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;
using MHStore.Repositories.Entities;

namespace MHStore.Services.ProductService;

public class Service : IService
{
    private readonly AppDbContext _context;

    public Service(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response>> GetAllAsync(string? search = null, string? category = null, bool includeUnavailable = false)
    {
        var query = _context.Products
            .AsNoTracking();

        if (!includeUnavailable)
        {
            query = query.Where(p => p.IsAvailable);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(keyword) ||
                (p.Description != null && p.Description.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(category) && category.Trim() != "Tat ca")
        {
            var selectedCategory = category.Trim().ToLower();
            query = query.Where(p => p.Category.ToLower() == selectedCategory);
        }

        var products = await query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return products.Select(ToResponse);
    }

    public async Task<Response?> GetByIdAsync(Guid id, bool includeUnavailable = false)
    {
        var query = _context.Products.AsNoTracking();

        if (!includeUnavailable)
        {
            query = query.Where(p => p.IsAvailable);
        }

        var product = await query.FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : ToResponse(product);
    }

    public async Task<Response> CreateAsync(Request request)
    {
        Validate(request);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            ImageUrl = request.ImageUrl?.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Khác" : request.Category.Trim(),
            IsAvailable = request.IsAvailable
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return ToResponse(product);
    }
    
    public async Task<Response?> UpdateAsync(Guid id, Request request)
    {
        Validate(request);

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return null;
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl?.Trim();
        product.Category = string.IsNullOrWhiteSpace(request.Category) ? "Khac" : request.Category.Trim();
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

    private static void Validate(Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Product name is required.");
        }

        if (request.Price <= 0)
        {
            throw new ArgumentException("Product price must be greater than zero.");
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
            Category = product.Category,
            IsAvailable = product.IsAvailable
        };
    }
}
