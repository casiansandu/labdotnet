using Microsoft.EntityFrameworkCore;
using ProductManagementt.Persistance;

namespace ProductManagementt.Features.Products;

public class GetAllProductsHandler(ProductManagementContext context)
{
    public async Task<IResult> Handle(GetAllProductsRequest request)
    {
        var products = await context.Products.ToListAsync();
        
        return Results.Ok(products);
    }
}