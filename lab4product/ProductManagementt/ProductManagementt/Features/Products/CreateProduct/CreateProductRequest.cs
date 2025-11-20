namespace ProductManagementt.Features.Products;

public record CreateProductRequest(
    string          Name,
    string          Brand,
    string          SKU,
    ProductCategory Category,
    decimal         Price,
    DateTime        ReleaseDate,
    DateTime        CreatedAt,
    string?         ImageUrl,
    bool            IsAvailable,
    int             StockQuantity = 0, 
    Guid            Id = new Guid());