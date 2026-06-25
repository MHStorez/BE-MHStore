namespace MHStore.Services.ProductService;

public class Request
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Category { get; set; }
    public string? NewCategoryName { get; set; }
    public bool IsAvailable { get; set; } = true;
}
