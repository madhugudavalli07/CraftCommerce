using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Entities;

namespace ProductService.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _dbContext;

    public ProductService(ProductDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            Stock = request.Stock,
            Images = CreateImages(request.ImageUrls)
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(MapToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Category = request.Category;
        product.Stock = request.Stock;
        _dbContext.ProductImages.RemoveRange(product.Images);
        product.Images = CreateImages(request.ImageUrls);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static ProductResponse MapToResponse(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        Category = product.Category,
        Stock = product.Stock,
        ImageUrls = product.Images
            .OrderBy(image => image.DisplayOrder)
            .Select(image => image.ImageUrl)
            .ToList()
    };

    private static List<ProductImage> CreateImages(IEnumerable<string> imageUrls) => imageUrls
        .Where(imageUrl => !string.IsNullOrWhiteSpace(imageUrl))
        .Select((imageUrl, index) => new ProductImage
        {
            Id = Guid.NewGuid(),
            ImageUrl = imageUrl.Trim(),
            DisplayOrder = index
        })
        .ToList();
}
