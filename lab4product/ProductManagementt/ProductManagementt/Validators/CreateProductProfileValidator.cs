using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProductManagementt.Persistance;
using ProductManagementt.Features.Products;
using System.Text.RegularExpressions;

namespace ProductManagementt.Validators;
public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private static readonly string[] InappropriateWords = { "badword1", "badword2", "offensive" }; // Extend this

    private static readonly string[] TechKeywords = { "Tech", "Smart", "Phone", "Tablet", "Laptop", "Digital", "Bluetooth" }; // Extend as needed

    private readonly ProductManagementContext _context;
    private readonly ILogger _logger;

    public CreateProductProfileValidator(ProductManagementContext context, ILogger<CreateProductHandler> logger)
    {
        _context = context;
        _logger = logger;

        // Name
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name must not be empty.")
            .Length(1, 200)
            .Must(BeValidName).WithMessage("Product name contains inappropriate content.")
            .MustAsync(BeUniqueName).WithMessage("Product name must be unique for the brand.");

        // Brand
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand name must not be empty.")
            .Length(2, 100)
            .Matches(@"^[a-zA-Z0-9\s\-'\.]+$").WithMessage("Brand contains invalid characters.");

        // SKU
        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU must not be empty.")
            .Must(BeValidSKU).WithMessage("SKU must be alphanumeric with hyphens, 5-20 characters.")
            .MustAsync(BeUniqueSKU).WithMessage("SKU must be unique.");

        // Category
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Category must be a valid value.");

        // Price
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .LessThan(10000).WithMessage("Price must be less than $10,000.");

        // ReleaseDate
        RuleFor(x => x.ReleaseDate)
            .Must(d => d >= new DateTime(1900, 1, 1)).WithMessage("Release date cannot be before 1900.")
            .Must(d => d <= DateTime.UtcNow).WithMessage("Release date cannot be in the future.");

        // StockQuantity
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(100000);

        // ImageUrl
        RuleFor(x => x.ImageUrl)
            .Must(BeValidImageUrl).When(x => !string.IsNullOrWhiteSpace(x.ImageUrl)).WithMessage("ImageUrl must be a valid HTTP/HTTPS image URL.");

        // -- Business Rules --

        RuleFor(x => x)
            .MustAsync(PassBusinessRules)
            .WithMessage("Business rules failed.");

        // -- Conditional & Cross-Field Validation (Task 3.3) --

        When(x => x.Category == ProductCategory.Electronics, () =>
        {
            RuleFor(x => x.Price).GreaterThanOrEqualTo(50.00m).WithMessage("Electronics price must be >= $50.");
            RuleFor(x => x.ReleaseDate).Must(d => d >= DateTime.UtcNow.AddYears(-5)).WithMessage("Electronics must be released within last 5 years.");
            RuleFor(x => x.Name).Must(ContainTechnologyKeywords).WithMessage("Electronics must contain technology keywords.");
        });

        When(x => x.Category == ProductCategory.Home, () =>
        {
            RuleFor(x => x.Price).LessThanOrEqualTo(200.00m).WithMessage("Home price must be <= $200.");
            RuleFor(x => x.Name).Must(BeAppropriateForHome).WithMessage("Home product name must be appropriate.");
        });

        When(x => x.Category == ProductCategory.Clothing, () =>
        {
            RuleFor(x => x.Brand)
                .MinimumLength(3)
                .WithMessage("Clothing brand name must be at least 3 characters.");
        });

        RuleFor(x => x)
            .Must(x => x.Price <= 100m || x.StockQuantity <= 20)
            .WithMessage("Expensive products (>$100) must have limited stock (≤20 units).");
    }

    private bool BeValidName(string name) =>
        !InappropriateWords.Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> BeUniqueName(CreateProductProfileRequest req, string name, CancellationToken token)
    {
        var exists = await _context.Products
            .AnyAsync(p => p.Name == name && p.Brand == req.Brand, cancellationToken: token);
        if (exists) _logger.LogWarning("Product name not unique: {Name} for brand {Brand}", name, req.Brand);
        return !exists;
    }

    private bool BeValidSKU(string sku)
    {
        var sk = sku.Replace(" ", "");
        return Regex.IsMatch(sk, @"^[a-zA-Z0-9\-]{5,20}$");
    }

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken token)
    {
        var exists = await _context.Products.AnyAsync(p => p.SKU == sku, cancellationToken: token);
        if (exists) _logger.LogWarning("SKU not unique: {SKU}", sku);
        return !exists;
    }

    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return (uri.Scheme == "http" || uri.Scheme == "https") &&
               (url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".png") 
                || url.EndsWith(".gif") || url.EndsWith(".webp"));
    }

    private async Task<bool> PassBusinessRules(CreateProductProfileRequest req, CancellationToken cancel)
    {
        // Rule 1: Daily addition check (max 500/day)
        var countToday = await _context.Products.CountAsync(p => p.CreatedAt.Date == DateTime.UtcNow.Date, cancel);
        if (countToday > 500)
        {
            _logger.LogWarning("Daily product addition limit reached");
            return false;
        }

        // Rule 2: Electronics min price ($50)
        if (req.Category == ProductCategory.Electronics && req.Price < 50)
        {
            _logger.LogWarning("Electronics min price violated");
            return false;
        }

        // Rule 3: Home name restrictions
        if (req.Category == ProductCategory.Home && InappropriateWords.Any(w => req.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Home product name contains restricted word");
            return false;
        }

        // Rule 4: High-value product stock limit
        if (req.Price > 500 && req.StockQuantity > 10)
        {
            _logger.LogWarning("Too much stock for expensive product");
            return false;
        }

        // Rule 5: Electronics must be recent
        if (req.Category == ProductCategory.Electronics && req.ReleaseDate < DateTime.UtcNow.AddYears(-5))
        {
            _logger.LogWarning("Electronics too old");
            return false;
        }
        return true;
    }

    private bool ContainTechnologyKeywords(CreateProductProfileRequest req, string name)
        => TechKeywords.Any(tk => name.Contains(tk, StringComparison.OrdinalIgnoreCase));

    private bool BeAppropriateForHome(CreateProductProfileRequest req, string name)
        => !InappropriateWords.Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase));
}