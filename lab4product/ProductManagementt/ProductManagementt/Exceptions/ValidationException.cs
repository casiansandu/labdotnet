using FluentValidation.Results;

namespace ProductManagementt.Exceptions
{
    public class ValidationException : Exception
    {
        public List<string> Errors { get; }

        public ValidationException(IEnumerable<ValidationFailure> failures)
            : base(string.Join("; ", failures.Select(f => f.ErrorMessage)))
        {
            Errors = failures.Select(f => f.ErrorMessage).ToList();
        }

        public ValidationException(string message) : base(message) { }
    }
}