namespace ProductService.DTOs;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Category { get; set; } = string.Empty;

    public int Stock { get; set; }

    public List<string> ImageUrls { get; set; } = [];
}
