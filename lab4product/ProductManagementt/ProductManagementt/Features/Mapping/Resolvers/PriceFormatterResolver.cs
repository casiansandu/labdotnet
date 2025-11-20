using System.Globalization;
using AutoMapper;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Features.Mapping.Resolvers;

public class PriceFormatterResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Price.ToString("C2", CultureInfo.CurrentCulture);
    }
}