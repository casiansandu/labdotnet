using AutoMapper;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Features.Mapping.Resolvers;

public class AvailabilityStatusResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable)
        {
            return "Out of Stock";
        }

        var stock = source.StockQuantity;

        switch (stock)
        {
            case <= 0:
                return "Unavailable";

            case 1:
                return "Last Item";

            case <= 5:
                return "Limited Stock";

            default:
                return "In Stock";
        }
    }
}
