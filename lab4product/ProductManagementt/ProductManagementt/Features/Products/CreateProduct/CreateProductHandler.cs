using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProductManagementt.Persistance;
using ProductManagementt.Features.Products;
using ProductManagementt.Common.Logging; // Adjust namespace to match your project
using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class CreateProductHandler(
    ProductManagementContext context,
    ILogger<CreateProductHandler> logger,
    IMapper mapper,
    IValidator<CreateProductProfileRequest> validator)
{
    public async Task<IResult> Handle(CreateProductProfileRequest request)
    {
        //correlation setup
        var operationId = Guid.NewGuid().ToString().Substring(0, 8);
        var correlationId = "";//
        if (context is IHttpContextAccessor accessor && accessor.HttpContext != null)
        {
            correlationId = accessor.HttpContext.Items["CorrelationId"] as string ?? "";
        }

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = operationId,
            ["CorrelationId"] = correlationId,
            ["ProductName"] = request.Name,
            ["SKU"] = request.SKU,
            ["Category"] = request.Category,
            ["Brand"] = request.Brand
        });

        var totalSw = Stopwatch.StartNew();
        logger.LogInformation(
            ProductLogEvents.ProductCreationStarted,
            "[Product] Creation started for {Name}, Brand: {Brand}, SKU: {SKU}, Category: {Category}",
            request.Name, request.Brand, request.SKU, request.Category);

        var validationSw = Stopwatch.StartNew();
        logger.LogInformation(ProductLogEvents.SKUValidationPerformed, "Validating SKU: {SKU}", request.SKU);
        logger.LogInformation(ProductLogEvents.StockValidationPerformed, "Validating stock for SKU: {SKU}", request.SKU);

        //alidation
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            validationSw.Stop();
            totalSw.Stop();

            logger.LogError(ProductLogEvents.ProductValidationFailed, "[Product] Validation failed for {Name} [{SKU}] ({Category})",
                request.Name, request.SKU, request.Category);

            var metrics = new ProductCreationMetrics(
                operationId,
                request.Name,
                request.SKU,
                request.Category,
                validationSw.Elapsed,
                TimeSpan.Zero,
                totalSw.Elapsed,
                false,
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))
            );
            logger.LogProductCreationMetrics(metrics);

            throw new ValidationException(validationResult.Errors);
        }

        // SKU uniqueness
        var skuExistsSw = Stopwatch.StartNew();
        var skuExists = await context.Products.AnyAsync(p => p.SKU == request.SKU);
        skuExistsSw.Stop();

        if (skuExists)
        {
            validationSw.Stop();
            totalSw.Stop();

            logger.LogError(ProductLogEvents.ProductValidationFailed, "SKU '{SKU}' already exists!", request.SKU);

            var metrics = new ProductCreationMetrics(
                operationId,
                request.Name,
                request.SKU,
                request.Category,
                validationSw.Elapsed + skuExistsSw.Elapsed,
                TimeSpan.Zero,
                totalSw.Elapsed,
                false,
                $"A product with SKU '{request.SKU}' already exists."
            );
            logger.LogProductCreationMetrics(metrics);
            
            throw new ValidationException($"A product with SKU '{request.SKU}' already exists.");
        }

        validationSw.Stop();

        // Database save timing
        var dbSw = Stopwatch.StartNew();
        logger.LogInformation(ProductLogEvents.DatabaseOperationStarted, "[Product] DB Save started for SKU: {SKU}", request.SKU);

        var product = mapper.Map<Product>(request);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        logger.LogInformation(ProductLogEvents.DatabaseOperationCompleted, "[Product] DB Save completed for SKU: {SKU}", request.SKU);
        dbSw.Stop();

        // Cache operation
        //logger.LogInformation(ProductLogEvents.CacheOperationPerformed, "[Product] Cache invalidation for key 'all_products'");

        var productDto = mapper.Map<ProductProfileDto>(product);

        totalSw.Stop();

        var metricsSuccess = new ProductCreationMetrics(
            operationId,
            product.Name,
            product.SKU,
            product.Category,
            validationSw.Elapsed,
            dbSw.Elapsed,
            totalSw.Elapsed,
            true,
            null
        );

        logger.LogProductCreationMetrics(metricsSuccess);

        logger.LogInformation(
            ProductLogEvents.ProductCreationCompleted,
            "Product created successfully. Id: {ProductId}, Name: {Name}, SKU: {SKU}, Category: {Category}",
            product.Id,
            product.Name,
            product.SKU,
            product.Category);

        return Results.Created($"/products/{productDto.Id}", productDto);
    }
}