namespace MHStore.Repositories.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string Category { get; set; } = "Khác";
    public bool IsAvailable { get; set; } = true;
}
