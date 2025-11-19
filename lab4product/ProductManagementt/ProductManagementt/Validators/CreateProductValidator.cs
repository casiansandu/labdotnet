using FluentValidation;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.Brand).NotEmpty().WithMessage("Brand is required");
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required");
        RuleFor(x => x.Category).NotEmpty().WithMessage("Category is required");
        RuleFor(x => x.Price).NotEmpty().WithMessage("Price is required");
        RuleFor(x => x.ReleaseDate).NotEmpty().WithMessage("ReleaseDate is required");
    }
}