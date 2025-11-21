using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProductManagementt.Features.Products;
using ProductManagementt.Persistance;
using Microsoft.Extensions.Logging;

namespace ProductManagementt.Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private readonly ProductManagementContext _context;
    private readonly ILogger<CreateProductProfileValidator> _logger;

    private static readonly string[] InappropriateWords = { "badword1", "badword2", "inappropriate" };
    private static readonly string[] RestrictedHomeWords = { "dangerous", "hazard", "explosive" };
    private static readonly string[] TechnologyKeywords = { "tech", "device", "smart", "digital" };

    public CreateProductProfileValidator(ProductManagementContext context, ILogger<CreateProductProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name cannot be empty.")
            .Length(1, 200)
            .MustAsync(BeUniqueName).WithMessage("Name must be unique for the brand.")
            .Must(BeValidName).WithMessage("Name contains inappropriate content.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand cannot be empty.")
            .Length(2, 100)
            .Must(BeValidBrandName).WithMessage("Brand contains invalid characters.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU cannot be empty.")
            .Must(BeValidSKU).WithMessage("SKU format is invalid.")
            .MustAsync(BeUniqueSKU).WithMessage("SKU already exists.");

        RuleFor(x => x.Category)
            .IsInEnum();

        RuleFor(x => x.Price)
            .GreaterThan(0).LessThan(10000);

        RuleFor(x => x.ReleaseDate)
            .LessThan(DateTime.UtcNow)
            .GreaterThan(new DateTime(1900, 1, 1));

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100_000);

        When(x => !string.IsNullOrWhiteSpace(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl)
                .Must(BeValidImageUrl)
                .WithMessage("ImageUrl must be HTTP/HTTPS and end with a valid image extension.");
        });

        RuleFor(x => x)
            .MustAsync(PassBusinessRules);

        // Conditional validations
        When(x => x.Category == ProductCategory.Electronics, () =>
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(50M);
            RuleFor(x => x.Name)
                .Must(ContainTechnologyKeywords).WithMessage("Electronics name must contain technology keywords.");
            RuleFor(x => x.ReleaseDate)
                .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-5)).WithMessage("Electronics must be released within 5 years.");
        });

        When(x => x.Category == ProductCategory.Home, () =>
        {
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(200M);
            RuleFor(x => x.Name)
                .Must(BeAppropriateForHome).WithMessage("Home product name is inappropriate.");
        });

        When(x => x.Category == ProductCategory.Clothing, () =>
        {
            RuleFor(x => x.Brand)
                .MinimumLength(3);
        });

        RuleFor(x => x)
            .Must(LimitedStockForExpensiveProduct).WithMessage("Expensive products (>$100) must have ≤20 units in stock.");
    }

    private bool BeValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return !InappropriateWords.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> BeUniqueName(CreateProductProfileRequest req, string name, CancellationToken ct)
    {
        var exists = await _context.Products.AnyAsync(p => p.Name == name && p.Brand == req.Brand, ct);
        if (exists)
            _logger.LogWarning("Name+Brand uniqueness failed for {Name}, {Brand}", name, req.Brand);
        return !exists;
    }

    private bool BeValidBrandName(string brand)
    {
        return Regex.IsMatch(brand, @"^[\w\s\-'.]+$");
    }

    private bool BeValidSKU(string sku)
    {
        sku = sku.Replace(" ", "");
        return Regex.IsMatch(sku, @"^[A-Za-z0-9-]{5,20}$");
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken ct)
    {
        var exists = await _context.Products.AnyAsync(p => p.SKU == sku, ct);
        if (exists)
            _logger.LogWarning("SKU uniqueness failed for {SKU}", sku);
        return !exists;
    }

    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        var valid = (url.StartsWith("http://") || url.StartsWith("https://")) &&
            (url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".png") || url.EndsWith(".gif") || url.EndsWith(".webp"));
        return valid;
    }

    private async Task<bool> PassBusinessRules(CreateProductProfileRequest req, CancellationToken ct)
    {
        var todayTotal = await _context.Products.CountAsync(p => p.CreatedAt.Date == DateTime.UtcNow.Date, ct);
        if (todayTotal > 500)
        {
            _logger.LogWarning("Daily product addition limit exceeded ({Count})", todayTotal);
            return false;
        }
        if (req.Category == ProductCategory.Electronics && req.Price < 50.0M)
        {
            _logger.LogWarning("Electronics price below minimum for {SKU}", req.SKU);
            return false;
        }
        if (req.Category == ProductCategory.Home && RestrictedHomeWords.Any(w => req.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Home product name violates restricted words: {Name}", req.Name);
            return false;
        }
        if (req.Price > 500M && req.StockQuantity > 10)
        {
            _logger.LogWarning("High value product has excessive stock {SKU}", req.SKU);
            return false;
        }
        return true;
    }

    private bool ContainTechnologyKeywords(CreateProductProfileRequest req, string name)
    {
        return TechnologyKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private bool BeAppropriateForHome(CreateProductProfileRequest req, string name)
    {
        return !RestrictedHomeWords.Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase));
    }

    private bool LimitedStockForExpensiveProduct(CreateProductProfileRequest req)
    {
        if (req.Price > 100M && req.StockQuantity > 20) return false;
        return true;
    }
}