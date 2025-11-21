using AutoMapper;
using ProductManagementt.Features.Mapping.Resolvers;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Common.Mapping;

public class AdvancedProductMappingProfile : Profile
{
    public AdvancedProductMappingProfile()
    {
        CreateMap<CreateProductProfileRequest, Product>()
            .ConstructUsing(src =>
                new Product(
                    Guid.NewGuid(),
                    src.Name,
                    src.Brand,
                    src.SKU,
                    src.Category,
                    src.Price,
                    src.ReleaseDate,
                    DateTime.UtcNow,
                    src.StockQuantity > 0,
                    src.ImageUrl,
                    src.StockQuantity
                )
            );

        CreateMap<Product, ProductProfileDto>()
            .ForMember(d => d.Id,           o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name,         o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Brand,        o => o.MapFrom(s => s.Brand))
            .ForMember(d => d.SKU,          o => o.MapFrom(s => s.SKU))
            .ForMember(d => d.ReleaseDate,  o => o.MapFrom(s => s.ReleaseDate))
            .ForMember(d => d.CreatedAt,    o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.IsAvailable,  o => o.MapFrom(s => s.IsAvailable))
            .ForMember(d => d.StockQuantity,o => o.MapFrom(s => s.StockQuantity))

            .ForMember(d => d.ImageUrl, o =>
                o.MapFrom(s =>
                    s.Category == ProductCategory.Home
                        ? null
                        : s.ImageUrl))

            .ForMember(d => d.Price, o =>
                o.MapFrom(s =>
                    s.Category == ProductCategory.Home
                        ? Math.Round(s.Price * 0.9m, 2)
                        : s.Price))

            .ForMember(d => d.CategoryDisplayName, o => o.MapFrom<CategoryDisplayResolver>())
            .ForMember(d => d.FormattedPrice,      o => o.MapFrom<PriceFormatterResolver>())
            .ForMember(d => d.ProductAge,          o => o.MapFrom<ProductAgeResolver>())
            .ForMember(d => d.BrandInitials,       o => o.MapFrom<BrandInitialsResolver>())
            .ForMember(d => d.AvailabilityStatus,  o => o.MapFrom<AvailabilityStatusResolver>());
    }
}