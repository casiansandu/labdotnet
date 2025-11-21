using AutoMapper;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Features.Mapping.Resolvers;

public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Brand))
        {
            return "?";
        }

        var parts = source.Brand
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][0].ToString().ToUpperInvariant();
        }

        var firstInitial = parts.First()[0];
        var lastInitial  = parts.Last()[0];

        return $"{char.ToUpperInvariant(firstInitial)}{char.ToUpperInvariant(lastInitial)}";
    }
}