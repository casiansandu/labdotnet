
namespace ProductManagementt.Features.Products;

public record Product(
    Guid Id,
    string          Name,
    string          Brand,
    string          SKU,
    ProductCategory Category,
    decimal         Price,
    DateTime        ReleaseDate,
    DateTime        CreatedAt,
    bool            IsAvailable,
    string?         ImageUrl = null,
    int             StockQuantity = 1);