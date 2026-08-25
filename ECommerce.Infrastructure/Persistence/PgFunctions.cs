namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Maps to the PostgreSQL f_unaccent(text) function created in the
/// AddSearchExtensions migration. Translated to SQL when used inside LINQ.
/// </summary>
public static class PgFunctions
{
    public static string Unaccent(string value)
        => throw new NotSupportedException("Only for use inside EF Core LINQ queries");
}
