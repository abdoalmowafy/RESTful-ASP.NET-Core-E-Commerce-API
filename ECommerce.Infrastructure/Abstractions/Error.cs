namespace ECommerce.Infrastructure.Abstractions;

public record Error(string Code, string Description, int? StatusCode)
{
    public static readonly Error None = new(string.Empty, string.Empty, null);

    public static Error BadRequest(string code, string description, int? statusCode = StatusCodes.Status400BadRequest)
        => new(code, description, statusCode);

    public static Error NotFound(string code, string description)
        => new(code, description, StatusCodes.Status404NotFound);

    public static Error Conflict(string code, string description)
        => new(code, description, StatusCodes.Status409Conflict);

    public static Error Forbidden(string code, string description)
        => new(code, description, StatusCodes.Status403Forbidden);

    public static Error Unauthorized(string code, string description)
        => new(code, description, StatusCodes.Status401Unauthorized);
}
