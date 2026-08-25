namespace ECommerce.Infrastructure.Extensions;

public static class LikeEscapeExtensions
{
    public static string EscapeLikePattern(this string input)
        => input.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    public static string StripDiacritics(this string value)
    {
        var formD = value.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(formD.Length);
        foreach (var c in formD)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
