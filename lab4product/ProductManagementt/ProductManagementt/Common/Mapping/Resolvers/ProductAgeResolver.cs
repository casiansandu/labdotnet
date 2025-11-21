using AutoMapper;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Features.Mapping.Resolvers;

public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var now = DateTime.UtcNow.Date;
        var releaseDate = source.ReleaseDate.Date;

        var totalDays = (now - releaseDate).TotalDays;
        var days = (int)Math.Floor(totalDays);

        if (days < 0)
        {
            return "New Release";
        }

        switch (days)
        {
            case < 30:
                return "New Release";

            case < 365:
            {
                var months = Math.Max(1, days / 30);
                return months == 1 ? "1 month old" : $"{months} months old";
            }

            case < 1825: //5 years
            {
                var years = Math.Max(1, days / 365);
                return years == 1 ? "1 year old" : $"{years} years old";
            }

            default:
                return "Classic";
        }
    }
}