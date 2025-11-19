namespace ProductManagementt.Middleware;

public class ErrorResponse
{
    public ErrorResponse(string errorCode, string message)
    {
        ErrorCode = errorCode;
        Message = message;
    }

    public ErrorResponse(string errorCode, string message, List<string> details) : this(errorCode, message)
    {
        Details = details;
    }

    public List<string> Details { get; set; } = new List<string>();

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;
}