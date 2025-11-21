using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ProductManagementt.Features.Products;
using ProductManagementt.Persistance;
using ProductManagementt.Validators;
using Microsoft.AspNetCore.Http.HttpResults;
using ProductManagementt.Common.Logging;
using ProductManagementt.Common.Mapping;
using ProductManagementt.Exceptions;
using ProductManagementt.Features.Products.CreateProduct;

public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ProductManagementContext _context;
    private readonly IMapper _mapper;
    private readonly Mock<ILogger<CreateProductHandler>> _loggerMock;
    private readonly CreateProductHandler _handler;
    private readonly IMemoryCache _cache;

    public CreateProductHandlerIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ProductManagementContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ProductManagementContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductMapping>();
            cfg.AddProfile<AdvancedProductMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _loggerMock = new Mock<ILogger<CreateProductHandler>>();

        _cache = new MemoryCache(new MemoryCacheOptions());

        var validator = new CreateProductProfileValidator(_context, _loggerMock.Object);

        _handler = new CreateProductHandler(_context, _loggerMock.Object, _mapper, validator, _cache);
    }

    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        var request = new CreateProductProfileRequest(
            "Samsung Galaxy S30",
            "Samsung Electronics",
            "SGS30-2025",
            ProductCategory.Electronics,
            899.99m,
            DateTime.UtcNow.AddDays(-10),
            "https://example.com/galaxy.jpg",
            3
        );

        var result = await _handler.Handle(request);
        var createdResult = result as Created<ProductProfileDto>;

        Assert.NotNull(createdResult);
        var dto = createdResult.Value;
        Assert.Equal("Electronics & Technology", dto.CategoryDisplayName);
        Assert.Equal("SE", dto.BrandInitials);
        Assert.Equal("New Release", dto.ProductAge);
        Assert.StartsWith("$", dto.FormattedPrice);
        Assert.Equal("Limited Stock", dto.AvailabilityStatus);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                2001, // ProductCreationStarted
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creation started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {
        var existingProduct = new Product(
            Guid.NewGuid(),
            "Existing Product",
            "TestBrand",
            "DUPSKU123",
            ProductCategory.Electronics,
            100m,
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow,
            true,
            null,
            5
        );
        await _context.Products.AddAsync(existingProduct);
        await _context.SaveChangesAsync();

        var request = new CreateProductProfileRequest(
            "New Product",
            "TestBrand",
            "DUPSKU123", // Duplicate SKU
            ProductCategory.Electronics,
            120m,
            DateTime.UtcNow.AddMonths(-1),
            "",
            3
        );

        var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => _handler.Handle(request)
        );

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.Is<EventId>(e => e.Id == ProductLogEvents.ProductValidationFailed),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SKU")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }


    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        var request = new CreateProductProfileRequest
        (
            "Home Product",
            "HomeBrand",
            "HOMESKU123",
            ProductCategory.Home,
            100m,
            DateTime.UtcNow.AddMonths(-2),
            "https://example.com/image.png",
            5
        );

        var result = await _handler.Handle(request);

        var createdResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<ProductProfileDto>>(result);
        var productDto = createdResult.Value;

        Assert.Equal(request.Price * 0.9m, productDto.Price);

        Assert.Equal("Home & Garden", productDto.CategoryDisplayName);

        Assert.Null(productDto.ImageUrl);

        Assert.Equal("Limited Stock", productDto.AvailabilityStatus);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.Is<EventId>(e => e.Id == ProductLogEvents.ProductCreationStarted),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(request.Name)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }


    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }
}
