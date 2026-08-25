namespace ECommerce.Authentication.Jwt;

public class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int LifetimeDays { get; set; } = 7;
    public int GraceSeconds { get; set; } = 5;
    public string CookieName { get; set; } = "ecommerce_rt";
    public string CookiePath { get; set; } = "/api/auth";
}
