using AutoMapper;
using ProductManagementt.Features.Mapping.Resolvers;
using ProductManagementt.Features.Products;
public class ProductMapping : Profile
{
    public ProductMapping()
    {
        CreateMap<Product, ProductProfileDto>()
            .ForMember(dest => dest.CategoryDisplayName,
                opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.FormattedPrice,
                opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.ProductAge,
                opt => opt.MapFrom<ProductAgeResolver>())
            .ForMember(dest => dest.BrandInitials,
                opt => opt.MapFrom<BrandInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus,
                opt => opt.MapFrom<AvailabilityStatusResolver>())
            // Direct mappings
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
            .ForMember(dest => dest.SKU, opt => opt.MapFrom(src => src.SKU))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.IsAvailable))
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity));
    }
}
