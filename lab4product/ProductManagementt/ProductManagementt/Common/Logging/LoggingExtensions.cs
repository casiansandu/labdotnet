namespace ProductManagementt.Common.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(this ILogger logger, ProductCreationMetrics metrics)
    {
        logger.LogInformation(
            ProductLogEvents.ProductCreationCompleted,
            "[Product] Operation {OperationId}: '{ProductName}' SKU: {SKU}, Category: {Category} | Validation: {ValidationDuration}ms, DB: {DatabaseSaveDuration}ms, Total: {TotalDuration}ms | Success: {Success} {ErrorReason}",
            metrics.OperationId,
            metrics.ProductName,
            metrics.SKU,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? ""
        );
    }
}