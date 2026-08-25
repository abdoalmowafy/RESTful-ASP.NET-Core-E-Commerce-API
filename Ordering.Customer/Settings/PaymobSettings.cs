namespace Ordering.Customer.Settings;

public class PaymobSettings
{
    public const string SectionName = "Paymob";

    public string ApiKey { get; set; } = string.Empty;
    public string IntegrationIdCard { get; set; } = string.Empty;
    public string IntegrationIdWallet { get; set; } = string.Empty;
    public string IframeId { get; set; } = string.Empty;
    public string HmacSecret { get; set; } = string.Empty;
}
