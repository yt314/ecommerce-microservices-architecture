namespace ECommerce.Monolith.Api.Services;

// Lets the service layer report success/not-found/validation outcomes without
// throwing exceptions for control flow or depending on ASP.NET types directly;
// controllers map this to the actual HTTP status.
public class ServiceResult<T>
{
    public bool Succeeded { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }
    public ServiceErrorKind ErrorKind { get; private init; }

    public static ServiceResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static ServiceResult<T> NotFound(string error) =>
        new() { Succeeded = false, Error = error, ErrorKind = ServiceErrorKind.NotFound };

    public static ServiceResult<T> Validation(string error) =>
        new() { Succeeded = false, Error = error, ErrorKind = ServiceErrorKind.Validation };
}

public enum ServiceErrorKind
{
    None,
    NotFound,
    Validation
}
