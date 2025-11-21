using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ProductManagementt.Persistance;
using ProductManagementt.Features.Products;
using ProductManagementt.Common.Mapping;
using ProductManagementt.Common.Logging;
using FluentValidation;
using System;
using Microsoft.AspNetCore.Http.HttpResults;
using ProductManagementt.Validators;

public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ProductManagementContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProductHandler> _logger;
    private readonly IValidator<CreateProductProfileRequest> _validator;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ProductManagementContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ProductManagementContext(options);

        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<ProductMapping>();
            cfg.AddProfile<AdvancedProductMappingProfile>();
        });
        _mapper = config.CreateMapper();
        _logger = new LoggerFactory().CreateLogger<CreateProductHandler>();
        _validator = new CreateProductProfileValidator(_context, _logger);
        _handler = new CreateProductHandler(_context, _logger, _mapper, _validator);
    }

    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        var request = new CreateProductProfileRequest
        (
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
    }

    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {
        var existing = new Product
        (
            Guid.NewGuid(),
            "Existing Laptop",
            "Lenovo",
            "LEN-12345",
            ProductCategory.Electronics,
            1499.99m,
            DateTime.UtcNow.AddMonths(-2),
            DateTime.UtcNow,
            true,
            "",
            9
        );
        _context.Products.Add(existing);
        await _context.SaveChangesAsync();

        var request = new CreateProductProfileRequest
        (
            "Another Laptop",
            "Lenovo",
            "LEN-12345",
            ProductCategory.Electronics,
            1299.99m,
            DateTime.UtcNow.AddDays(-5),
            "",
            9
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(request));
        Assert.Contains("already exists", ex.Message);
        // With a log mock: verify ProductValidationFailed log was called
    }

    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        // Arrange
        var request = new CreateProductProfileRequest
        (
            "Cozy Sofa",
            "Ikea",
            "HOME-IK-2025",
            ProductCategory.Home,
            300.00m,
            DateTime.UtcNow.AddDays(-400),
            "https://example.com/sofa.jpg",
            41
        );

        var result = await _handler.Handle(request);
        var createdResult = result as Created<ProductProfileDto>;
        Assert.NotNull(createdResult);
        var dto = createdResult.Value;
        
        Assert.Equal("Home & Garden", dto.CategoryDisplayName);
        Assert.Equal(270.00m, dto.Price);
        Assert.Null(dto.ImageUrl);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}