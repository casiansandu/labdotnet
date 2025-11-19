using FluentValidation;
using ProductManagementt.Persistance;

namespace ProductManagementt.Features.Products;


public class CreateProductHandler(ProductManagementContext context, ILogger<CreateProductHandler> logger, IValidator<CreateProductRequest> validator)
{
    public async Task<IResult> Handle(CreateProductRequest request)
    {
        logger.LogInformation($"creating product with values:" +
                              $"name: {request.Name}\n" +
                              $"brand: {request.Brand}\n" +
                              $"price: {request.Price}\n" +
                              $"category: {request.Category}\n" +
                              $"{request.ReleaseDate}\n" +
                              $"quantity:{request.StockQuantity}\n");
        
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        var product = new Product(
            request.Id,
            request.Name,
            request.Brand,
            request.SKU,
            request.Category,
            request.Price,
            request.ReleaseDate,
            request.ImageUrl,
            request.StockQuantity);
        
        context.Products.Add(product);
        await context.SaveChangesAsync();
        logger.LogInformation($"Product created successfully with id: {product.Id}");

        return Results.Created($"/products/{product.Id}", product);
    }
}