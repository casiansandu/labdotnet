using AutoMapper;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Features.Mapping.Resolvers;

public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var now  = DateTime.UtcNow;
        var days = (now.Date - source.ReleaseDate.Date).TotalDays;

        if (days < 0)
        {
            // Should not happen if validation is correct, but just in case:
            return "New Release";
        }

        if (days < 30)
        {
            return "New Release";
        }

        if (days < 365)
        {
            var months = Math.Max(1, (int)(days / 30));
            return months == 1 ? "1 month old" : $"{months} months old";
        }

        if (days < 1825) // < 5 years
        {
            var years = Math.Max(1, (int)(days / 365));
            return years == 1 ? "1 year old" : $"{years} years old";
        }

        // 5+ years
        return "Classic";
    }
}