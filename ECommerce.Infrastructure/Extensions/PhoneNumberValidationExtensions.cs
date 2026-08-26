using PhoneNumbers;

namespace ECommerce.Infrastructure.Extensions;

/// <summary>
/// Real phone-number validation via Google's libphonenumber.
/// Local numbers are parsed against the store's default region (EG);
/// international formats ("+44 …") parse against any region.
/// </summary>
public static class PhoneNumberValidationExtensions
{
    private const string DefaultRegion = "EG";
    private static readonly PhoneNumberUtil Util = PhoneNumberUtil.GetInstance();

    public static bool IsValidPhone(this string? value, string defaultRegion = DefaultRegion)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var number = Util.Parse(value, defaultRegion);
            return Util.IsValidNumber(number);
        }
        catch (NumberParseException)
        {
            return false;
        }
    }

    /// <summary>Normalizes to E.164 (e.g. +201098765432) for canonical storage.</summary>
    public static string? ToE164(this string? value, string defaultRegion = DefaultRegion)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        try
        {
            return Util.Format(Util.Parse(value, defaultRegion), PhoneNumberFormat.E164);
        }
        catch (NumberParseException)
        {
            return value;
        }
    }
}
