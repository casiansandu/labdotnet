using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ProductManagementt.Persistance;
using ProductManagementt.Features.Products;
using ProductManagementt.Common.Logging;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ProductManagementt.Features.Products.CreateProduct
{
    public class CreateProductHandler
    {
        private readonly ProductManagementContext _context;
        private readonly ILogger<CreateProductHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateProductProfileRequest> _validator;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _accessor;

        public CreateProductHandler(
            ProductManagementContext context,
            ILogger<CreateProductHandler> logger,
            IMapper mapper,
            IValidator<CreateProductProfileRequest> validator,
            IMemoryCache cache,
            IHttpContextAccessor accessor = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _accessor = accessor;
        }

        public async Task<IResult> Handle(CreateProductProfileRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // correlation setup
            var operationId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var correlationId = "";
            if (_accessor?.HttpContext != null && _accessor.HttpContext.Items.ContainsKey("CorrelationId"))
            {
                correlationId = _accessor.HttpContext.Items["CorrelationId"] as string ?? "";
            }

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["OperationId"] = operationId,
                ["CorrelationId"] = correlationId,
                ["ProductName"] = request.Name,
                ["SKU"] = request.SKU,
                ["Category"] = request.Category,
                ["Brand"] = request.Brand
            });

            var totalSw = Stopwatch.StartNew();

            _logger.LogInformation(new EventId(ProductLogEvents.ProductCreationStarted),
                "[Product] Creation started for {Name}, Brand: {Brand}, SKU: {SKU}, Category: {Category}",
                request.Name, request.Brand, request.SKU, request.Category);

            var validationSw = Stopwatch.StartNew();

            _logger.LogInformation(new EventId(ProductLogEvents.SKUValidationPerformed), "Validating SKU: {SKU}", request.SKU);
            _logger.LogInformation(new EventId(ProductLogEvents.StockValidationPerformed), "Validating stock for SKU: {SKU}", request.SKU);

            // validation
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                validationSw.Stop();
                totalSw.Stop();

                _logger.LogError(new EventId(ProductLogEvents.ProductValidationFailed),
                    "[Product] Validation failed for {Name} [{SKU}] ({Category})", request.Name, request.SKU, request.Category);

                var metricsFail = new ProductCreationMetrics(
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

                _logger.LogProductCreationMetrics(metricsFail);

                throw new ValidationException(validationResult.Errors);
            }

            // SKU uniqueness check
            var skuExistsSw = Stopwatch.StartNew();
            var skuExists = await _context.Products.AnyAsync(p => p.SKU == request.SKU);
            skuExistsSw.Stop();

            if (skuExists)
            {
                validationSw.Stop();
                totalSw.Stop();

                _logger.LogError(new EventId(ProductLogEvents.ProductValidationFailed), "SKU '{SKU}' already exists!", request.SKU);

                var metricsDup = new ProductCreationMetrics(
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

                _logger.LogProductCreationMetrics(metricsDup);

                throw new ValidationException($"A product with SKU '{request.SKU}' already exists.");
            }

            validationSw.Stop();

            // Database save timing
            var dbSw = Stopwatch.StartNew();
            _logger.LogInformation(new EventId(ProductLogEvents.DatabaseOperationStarted), "[Product] DB Save started for SKU: {SKU}", request.SKU);

            var product = _mapper.Map<Product>(request);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation(new EventId(ProductLogEvents.DatabaseOperationCompleted), "[Product] DB Save completed for SKU: {SKU}", request.SKU);
            dbSw.Stop();

            // Cache operation - invalidate "all_products"
            try
            {
                _cache.Remove("all_products");
                _logger.LogInformation(new EventId(ProductLogEvents.CacheOperationPerformed), "[Product] Cache invalidated: all_products");
            }
            catch (Exception ex)
            {
                // Cache failures shouldn't break creation; log as warning
                _logger.LogWarning(ex, "Failed to invalidate product cache for key 'all_products'");
            }

            var productDto = _mapper.Map<ProductProfileDto>(product);

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

            _logger.LogProductCreationMetrics(metricsSuccess);

            _logger.LogInformation(new EventId(ProductLogEvents.ProductCreationCompleted),
                "Product created successfully. Id: {ProductId}, Name: {Name}, SKU: {SKU}, Category: {Category}",
                product.Id,
                product.Name,
                product.SKU,
                product.Category);

            return Results.Created($"/products/{productDto.Id}", productDto);
        }
    }
}
