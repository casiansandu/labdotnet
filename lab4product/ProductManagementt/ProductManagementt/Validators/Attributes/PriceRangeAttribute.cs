using System.ComponentModel.DataAnnotations;

namespace ProductManagementt.Validators.Attributes;

public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _min, _max;
    public PriceRangeAttribute(double min, double max)
    {
        _min = (decimal)min; _max = (decimal)max;
    }

    public override bool IsValid(object? value)
        => value is decimal price && price >= _min && price <= _max;
}