using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Data;
using MHStore.Repositories.Entities;

namespace MHStore.Services.CategoryService;

public class Service : IService
{
    private readonly AppDbContext _context;

    public Service(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Categories.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(category => category.Status == "Active");
        }

        var categories = await query
            .OrderBy(category => category.Name)
            .ToListAsync();

        return categories.Select(ToResponse);
    }

    public async Task<Response> CreateAsync(Request request)
    {
        if (request == null)
        {
            throw new ArgumentException("Category request is required.");
        }

        var name = request.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.");
        }

        var normalizedName = name.ToLower();
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(category => category.Name.ToLower() == normalizedName);

        if (existingCategory != null)
        {
            throw new ArgumentException("Category name already exists.");
        }

        var status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim();

        if (status != "Active" && status != "Inactive")
        {
            throw new ArgumentException("Category status must be Active or Inactive.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = ToSlug(name),
            Status = status
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        return ToResponse(category);
    }

    private static Response ToResponse(Category category)
    {
        return new Response
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Status = category.Status
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
