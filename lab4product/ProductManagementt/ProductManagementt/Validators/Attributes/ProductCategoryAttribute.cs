using System.ComponentModel.DataAnnotations;
using ProductManagementt.Features.Products;

namespace ProductManagementt.Validators.Attributes;

public class ProductCategoryAttribute : ValidationAttribute
{
    private readonly ProductCategory[] _allowed;

    public ProductCategoryAttribute(params ProductCategory[] allowed)
    {
        _allowed = allowed;
    }

    public override bool IsValid(object? value)
        => value is ProductCategory cat && _allowed.Contains(cat);
}