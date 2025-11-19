namespace ProductManagementt.Exceptions;

public class ValidationException : BaseException
{
    public List<string> Errors { get; }

    public ValidationException(IEnumerable<string> errors) :
        base("Validation failed", 400, "VALIDATION_ERROR")
    {
        Errors = errors?.ToList() ?? new List<string>();
    }

    public ValidationException(string error) : base("Validation failed", 400, "VALIDATION_ERROR")
    {
        Errors = new List<string> { error };
    }
}