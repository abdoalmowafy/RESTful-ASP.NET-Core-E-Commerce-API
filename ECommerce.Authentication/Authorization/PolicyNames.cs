namespace ECommerce.Authentication.Authorization;

public static class PolicyNames
{
    /// <summary>Email or phone number confirmed.</summary>
    public const string VerifiedUser = "VerifiedUser";

    /// <summary>Customer with an active profile.</summary>
    public const string ActiveCustomer = "ActiveCustomer";

    /// <summary>Seller whose store is approved and active.</summary>
    public const string ActiveSeller = "ActiveSeller";

    /// <summary>Driver application pending verification.</summary>
    public const string PendingDriver = "PendingDriver";

    /// <summary>Approved, delivery-eligible driver.</summary>
    public const string ActiveDriver = "ActiveDriver";
}
