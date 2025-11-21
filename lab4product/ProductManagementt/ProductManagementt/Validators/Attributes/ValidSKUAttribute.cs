using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.RegularExpressions;

namespace ProductManagementt.Validators.Attributes;

public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        var sku = value.ToString()?.Replace(" ", "");
        return sku != null && Regex.IsMatch(sku, @"^[a-zA-Z0-9\-]{5,20}$");
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.Add("data-val", "true");
        context.Attributes.Add("data-val-validsku", ErrorMessage ?? "SKU format is invalid.");
    }
}