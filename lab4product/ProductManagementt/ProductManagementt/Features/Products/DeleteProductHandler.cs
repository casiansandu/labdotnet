using Microsoft.EntityFrameworkCore;
using ProductManagementt.Persistance;

namespace ProductManagementt.Features.Products;

public class DeleteProductHandler(ProductManagementContext context)
{
    public async Task<IResult> Handle(DeleteProductRequest request)
    {
        var product = await context.Products.FirstOrDefaultAsync( u=>u.Id == request.Id);
        if (product == null)
        {
            return Results.NotFound();
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return Results.NoContent();
    }
}