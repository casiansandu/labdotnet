using Microsoft.EntityFrameworkCore;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Persistance;

public class ProductManagementContext(DbContextOptions<ProductManagementContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
}