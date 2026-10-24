using System.Net;

namespace SchoolBuffet.Api.Services;

public class AppException : Exception
{
    public AppException(HttpStatusCode statusCode, string message, Dictionary<string, List<string>>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public HttpStatusCode StatusCode { get; }

    public Dictionary<string, List<string>>? Errors { get; }
}

public static class AppThrow
{
    public static void BadRequest(string message, Dictionary<string, List<string>>? errors = null)
    {
        throw new AppException(HttpStatusCode.BadRequest, message, errors);
    }

    public static void Unauthorized(string message)
    {
        throw new AppException(HttpStatusCode.Unauthorized, message);
    }

    public static void Forbidden(string message)
    {
        throw new AppException(HttpStatusCode.Forbidden, message);
    }

    public static void NotFound(string message)
    {
        throw new AppException(HttpStatusCode.NotFound, message);
    }

    public static void Conflict(string message, Dictionary<string, List<string>>? errors = null)
    {
        throw new AppException(HttpStatusCode.Conflict, message, errors);
    }
}

public sealed class ValidationBag
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool IsValid => _errors.Count == 0;

    public Dictionary<string, List<string>> Errors => _errors;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out List<string>? list))
        {
            list = new List<string>();
            _errors[field] = list;
        }

        list.Add(message);
    }

    public void Required(string field, string? value, string label, int max, int min = 1)
    {
        string trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length < min)
        {
            Add(field, $"{label} eshte i detyueshem");
            return;
        }

        if (trimmed.Length > max)
        {
            Add(field, $"{label} nuk mund te tejkaloje {max} shkronja");
        }
    }

    public void Ensure(bool condition, string field, string message)
    {
        if (!condition)
        {
            Add(field, message);
        }
    }

    public void ThrowIfInvalid(string message)
    {
        if (!IsValid)
        {
            AppThrow.BadRequest(message, _errors);
        }
    }
}
