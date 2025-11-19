namespace ProductManagementt.Features.Products;

public record CreateProductRequest(
    string          Name,
    string          Brand,
    string          SKU,
    ProductCategory Category,
    decimal         Price,
    DateTime        ReleaseDate,
    string?         ImageUrl,
    int             StockQuantity = 0, 
    Guid            Id = new Guid());